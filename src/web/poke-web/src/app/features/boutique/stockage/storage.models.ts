// ── Enums (miroir backend, sérialisés en PascalCase) ────────────────────────
export type LabelOrigin = 'Supplier' | 'Internal';
export type MovementStatus = 'Pending' | 'Confirmed' | 'Rejected';
export type CompileJobStatus = 'Queued' | 'Running' | 'Succeeded' | 'Failed';
export type FurnitureType = 'ShelfClassic6' | 'ShelfWall';

export const LABEL_ORIGIN_LABELS: Record<LabelOrigin, string> = {
  Supplier: 'Fournisseur (code-barre)',
  Internal: 'Interne (code maison)',
};

export const FURNITURE_LABELS: Record<FurnitureType, string> = {
  ShelfClassic6: 'Étagère (6 niveaux)',
  ShelfWall: 'Étagère murale',
};

export const BOX_SIZES = ['S', 'M', 'L', 'XL'] as const;
export type BoxSize = (typeof BOX_SIZES)[number];

// ── Assets 3D fixes (servis en statique, pas d'upload) ──────────────────────
export const STORAGE_ASSETS = {
  cartonClosed: '/assets/3d/storage/carton_closed.glb',
  cartonOpen:   '/assets/3d/storage/carton_open.glb',
  binderClosed: '/assets/3d/storage/binder_closed.glb',
  binderOpen:   '/assets/3d/storage/binder_open.glb',
  shelfClassic: '/assets/3d/storage/shelf_classic.glb',
  shelfWall:    '/assets/3d/storage/shelf_wall.glb',
} as const;

export const FURNITURE_ASSET: Record<FurnitureType, string> = {
  ShelfClassic6: STORAGE_ASSETS.shelfClassic,
  ShelfWall: STORAGE_ASSETS.shelfWall,
};

/**
 * Hauteur (Y, en mètres) de la surface de chaque niveau, relative à la base du meuble.
 * Valeurs issues de la génération Blender des modèles.
 */
export const FURNITURE_LEVEL_Y: Record<FurnitureType, number[]> = {
  ShelfClassic6: [0.13, 0.58, 1.03, 1.48, 1.93, 2.38],
  ShelfWall: [0.92],
};

/** Demi-largeur utile (X) d'un meuble, pour répartir les items sur un niveau. */
export const FURNITURE_HALF_WIDTH: Record<FurnitureType, number> = {
  ShelfClassic6: 0.35,
  ShelfWall: 0.9,
};

/** Payload émis quand un carton est déposé (drag&drop) sur le plan 3D (sol). */
export interface BoxDropEvent {
  boxId: string;
  x: number;
  y: number;
  z: number;
}

/** Payload émis quand un item (carton/classeur) est déposé sur un niveau de meuble. */
export interface FurnitureDropEvent {
  kind: 'box' | 'binder';
  itemId: string;
  furnitureId: string;
  levelIndex: number;
}

/** Payload émis quand un meuble est déposé sur le sol. */
export interface FurnitureFloorDropEvent {
  furnitureId: string;
  x: number;
  y: number;
  z: number;
}

/** Payload émis quand un classeur est déposé sur un carton (rangé dedans). */
export interface BinderBoxDropEvent {
  binderId: string;
  boxId: string;
}

// ── DTOs ────────────────────────────────────────────────────────────────────
export interface StorageRoomDto {
  id: string;
  shopId: string;
  name: string;
  floorPlanUrl: string | null;
  model3dUrl: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface StorageBoxDto {
  id: string;
  roomId: string;
  labelOrigin: LabelOrigin;
  barcode: string | null;
  internalCode: string | null;
  supplierName: string | null;
  size: string | null;
  position3dJson: string | null;
  notes: string | null;
  furnitureId: string | null;
  levelIndex: number | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface StorageBinderDto {
  id: string;
  boxId: string | null;
  label: string;
  positionInBox: number | null;
  furnitureId: string | null;
  levelIndex: number | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface StoragePricePointDto {
  source: string;
  price: number;
  currency: string;
  fetchedAt: string;
}

export interface StorageCardDetailDto {
  cardId: string | null;
  name: string;
  setName: string | null;
  imageSmall: string | null;
  imageLarge: string | null;
  type: string;
  condition: string | null;
  gradingCompany: string | null;
  gradeValue: number | null;
  estimatedPrice: number | null;
  estimatedCurrency: string | null;
  quantityAvailable: number;
  recentPrices: StoragePricePointDto[];
}

export interface StorageBinderCardDto {
  locationId: string;
  ownerType: string;
  ownerId: string;
  cardId: string | null;
  name: string;
  imageSmall: string | null;
  imageLarge: string | null;
  setName: string | null;
  condition: string | null;
  gradingCompany: string | null;
  gradeValue: number | null;
  slotPosition: number;
}

export interface StorageFurnitureDto {
  id: string;
  roomId: string;
  type: FurnitureType;
  name: string | null;
  levelCount: number;
  position3dJson: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface StorageLocationDto {
  id: string;
  binderId: string;
  ownerType: string;
  ownerId: string;
  slotPosition: number | null;
  movedAt: string;
}

export interface StorageBoxWithBindersDto {
  box: StorageBoxDto;
  binders: StorageBinderDto[];
}

export interface StorageRoomLayoutDto {
  room: StorageRoomDto;
  boxes: StorageBoxWithBindersDto[];
  furniture: StorageFurnitureDto[];
  looseBinders: StorageBinderDto[];
}

export interface StorageBinderWithLocationsDto {
  binder: StorageBinderDto;
  locations: StorageLocationDto[];
}

export interface StorageBoxLookupDto {
  box: StorageBoxDto;
  binders: StorageBinderWithLocationsDto[];
}

export interface StorageMovementEventDto {
  id: string;
  boxId: string | null;
  detectedAt: string;
  fromPosition3dJson: string | null;
  toPosition3dJson: string | null;
  status: MovementStatus;
  confirmedBy: string | null;
  confirmedAt: string | null;
  createdAt: string;
}

export interface StorageCompileJobDto {
  id: string;
  targetType: string;
  targetId: string;
  status: CompileJobStatus;
  glbOutputPath: string | null;
  errorMessage: string | null;
  createdAt: string;
  completedAt: string | null;
}

// ── Shop Stock (pour popup assignation) ──────────────────────────────────────
export interface ShopStockDto {
  id: string;
  shopId: string;
  cardId: string | null;
  cardName: string | null;
  cardImage: string | null;
  productName: string | null;
  type: string;
  language: string;
  condition: string | null;
  gradingCompany: string | null;
  gradeValue: number | null;
  quantityTotal: number;
  quantityAvailable: number;
  quantityReserved: number;
  purchasePrice: number | null;
  purchaseCurrency: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ── Scan classeur ─────────────────────────────────────────────────────────────
export interface CardScanMatchDto {
  cardId: string | null;
  cardName: string | null;
  setName: string | null;
  cardNumber: string | null;
  imageSmall: string | null;
  gradingCompany: string | null;
  gradeValue: number | null;
  certNumber: string | null;
  confidence: number;
  matched: boolean;
}

export interface ScanBinderResult {
  matches: CardScanMatchDto[];
}

// ── Paramètres module Stockage ───────────────────────────────────────────────
export interface StorageSettingsDto {
  /** Suppression en chaîne (cascade) activée. */
  cascadeDelete: boolean;
}

/** Valeur estimée d'un carton/classeur (somme des cotes des cartes rangées). */
export interface StorageEstimateDto {
  total: number | null;
  currency: string | null;
  itemCount: number;
}

// ── Requests ─────────────────────────────────────────────────────────────────
export interface CreateStorageRoomRequest {
  name: string;
}

export interface CreateStorageBoxRequest {
  roomId: string;
  labelOrigin: LabelOrigin;
  barcode: string | null;
  internalCode: string | null;
  supplierName: string | null;
  size: string | null;
  position3dJson: string | null;
  notes: string | null;
}

export interface CreateStorageBinderRequest {
  label: string;
  boxId: string | null;
  positionInBox: number | null;
  furnitureId: string | null;
  levelIndex: number | null;
}

export interface CreateStorageFurnitureRequest {
  roomId: string;
  type: FurnitureType;
  name: string | null;
}

/** Modèle d'affichage du popup récapitulatif d'un objet sélectionné (carton/meuble/classeur). */
export interface StorageRecapModel {
  kind: 'box' | 'furniture' | 'binder';
  /** Identifiant de l'objet (pour les actions Ouvrir / Supprimer). */
  id: string;
  /** Libellé principal (code carton, nom meuble, libellé classeur). */
  title: string;
  /** Type lisible (« Carton fournisseur », « Étagère 6 niveaux », « Classeur »). */
  subtitle: string;
  /** Contenance lisible (« 3 classeurs », « 6 niveaux », « position 4 »). */
  contenance: string;
  /** Prix estimé formaté, ou null si indisponible (agrégat backend à venir). */
  estimatedPrice: string | null;
  /** Code-barre affiché uniquement pour un carton fournisseur. */
  barcode: string | null;
  /** Indique si l'objet est placé dans la salle. */
  placed: boolean;
}

/** Position 3D d'un carton/meuble persistée en JSON. */
export interface Box3dPosition {
  x: number;
  y: number;
  z: number;
  rotation: number;
}
