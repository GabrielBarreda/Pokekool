// ── Enums (miroir backend, sérialisés en PascalCase) ────────────────────────
export type LabelOrigin = 'Supplier' | 'Internal';
export type MovementStatus = 'Pending' | 'Confirmed' | 'Rejected';
export type CompileJobStatus = 'Queued' | 'Running' | 'Succeeded' | 'Failed';

export const LABEL_ORIGIN_LABELS: Record<LabelOrigin, string> = {
  Supplier: 'Fournisseur (code-barre)',
  Internal: 'Interne (code maison)',
};

export const BOX_SIZES = ['S', 'M', 'L', 'XL'] as const;
export type BoxSize = (typeof BOX_SIZES)[number];

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
  createdAt: string;
  updatedAt: string | null;
}

export interface StorageBinderDto {
  id: string;
  boxId: string;
  label: string;
  positionInBox: number | null;
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
  boxId: string;
  label: string;
  positionInBox: number | null;
}

/** Position 3D d'un carton persistée en JSON. */
export interface Box3dPosition {
  x: number;
  y: number;
  z: number;
  rotation: number;
}
