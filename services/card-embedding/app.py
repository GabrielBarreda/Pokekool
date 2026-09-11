"""
Service d'embedding visuel pour le matching de cartes Pokémon.

Encode une image (photo de scan OU image officielle de catalogue) en vecteur
via DINOv2 (Meta AI, licence Apache 2.0 — https://github.com/facebookresearch/dinov2),
pour permettre une recherche par similarité (nearest neighbor) plutôt qu'une
identification texte pure via LLM vision.

Modèle : dinov2_vits14 (le plus léger de la famille DINOv2 — suffisant pour un
catalogue fermé de cartes imprimées, et raisonnable en CPU si pas de GPU dispo).
Dimension du vecteur produit : 384.

Endpoints :
  GET  /health          → statut du service + modèle chargé
  POST /embed            → { "imageBase64": "..." } -> { "embedding": [0.01, -0.23, ...], "dim": 384 }
"""
import base64
import io
import logging

import torch
from fastapi import FastAPI, HTTPException
from PIL import Image
from pydantic import BaseModel

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("card-embedding")

MODEL_NAME = "dinov2_vits14"
EMBEDDING_DIM = 384

app = FastAPI(title="Pokekool Card Embedding Service", version="1.0.0")

_model = None
_transform = None
_device = "cuda" if torch.cuda.is_available() else "cpu"


def _load_model():
    """Charge DINOv2 une seule fois au démarrage (paresseux au premier appel sinon)."""
    global _model, _transform
    if _model is not None:
        return

    from torchvision import transforms

    logger.info("Chargement de %s sur %s...", MODEL_NAME, _device)
    _model = torch.hub.load("facebookresearch/dinov2", MODEL_NAME)
    _model.eval()
    _model.to(_device)

    # Preprocessing standard DINOv2 : resize 224, normalisation ImageNet.
    _transform = transforms.Compose([
        transforms.Resize(256, interpolation=transforms.InterpolationMode.BICUBIC),
        transforms.CenterCrop(224),
        transforms.ToTensor(),
        transforms.Normalize(
            mean=[0.485, 0.456, 0.406],
            std=[0.229, 0.224, 0.225],
        ),
    ])
    logger.info("Modèle %s chargé sur %s.", MODEL_NAME, _device)


@app.on_event("startup")
def on_startup():
    # Chargement au démarrage plutôt qu'au premier appel : le premier scan réel
    # n'a pas à payer le coût de téléchargement/chargement du modèle.
    try:
        _load_model()
    except Exception:
        # Ne bloque pas le démarrage du service : /health signalera le problème,
        # et /embed retournera une 503 explicite plutôt que de planter le container.
        logger.exception("Échec du chargement du modèle au démarrage — sera retenté au premier appel.")


class EmbedRequest(BaseModel):
    imageBase64: str


class EmbedResponse(BaseModel):
    embedding: list[float]
    dim: int


@app.get("/health")
def health():
    return {
        "status": "ok" if _model is not None else "model_not_loaded",
        "model": MODEL_NAME,
        "device": _device,
        "dim": EMBEDDING_DIM,
    }


@app.post("/embed", response_model=EmbedResponse)
def embed(req: EmbedRequest):
    try:
        _load_model()
    except Exception as exc:
        logger.exception("Impossible de charger le modèle")
        raise HTTPException(status_code=503, detail=f"Modèle indisponible: {exc}") from exc

    try:
        raw = base64.b64decode(req.imageBase64)
        image = Image.open(io.BytesIO(raw)).convert("RGB")
    except Exception as exc:
        raise HTTPException(status_code=400, detail=f"Image illisible: {exc}") from exc

    try:
        tensor = _transform(image).unsqueeze(0).to(_device)
        with torch.no_grad():
            vector = _model(tensor)
        vector = vector.squeeze(0).cpu().tolist()
    except Exception as exc:
        logger.exception("Échec de l'encodage")
        raise HTTPException(status_code=500, detail=f"Échec de l'encodage: {exc}") from exc

    return EmbedResponse(embedding=vector, dim=len(vector))
