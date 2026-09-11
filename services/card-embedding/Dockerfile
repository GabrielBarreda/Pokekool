FROM python:3.11-slim

WORKDIR /app

# torch/torchvision CPU-only par défaut (le compose peut ajouter la section GPU
# comme pour le service ollama si un GPU NVIDIA + nvidia-container-toolkit est dispo).
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgl1 \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app.py .

# Le poids DINOv2 est téléchargé au premier démarrage (torch.hub) et mis en cache
# dans /root/.cache/torch — monter ce chemin en volume évite de re-télécharger
# à chaque redémarrage du container (voir docker-compose.yml).
ENV TORCH_HOME=/root/.cache/torch

EXPOSE 8500

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8500"]
