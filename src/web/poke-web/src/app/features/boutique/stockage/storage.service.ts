import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  StorageRoomDto, StorageRoomLayoutDto, StorageBoxDto, StorageBinderDto,
  StorageLocationDto, StorageBoxLookupDto, StorageMovementEventDto, StorageCompileJobDto,
  StorageFurnitureDto, StorageBinderCardDto, StorageCardDetailDto, StorageSettingsDto, StorageEstimateDto,
  CreateStorageRoomRequest, CreateStorageBoxRequest, CreateStorageBinderRequest,
  CreateStorageFurnitureRequest, ShopStockDto, PagedResult, ScanBinderResult,
} from './storage.models';

@Injectable({ providedIn: 'root' })
export class StorageService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/v1/admin/storage`;
  private readonly shopBase = `${environment.apiBaseUrl}/v1/admin/shop`;

  // ── Salles ─────────────────────────────────────────────
  getRooms(): Observable<StorageRoomDto[]> {
    return this.http.get<StorageRoomDto[]>(`${this.base}/rooms`);
  }
  createRoom(body: CreateStorageRoomRequest): Observable<StorageRoomDto> {
    return this.http.post<StorageRoomDto>(`${this.base}/rooms`, body);
  }
  getRoomLayout(roomId: string): Observable<StorageRoomLayoutDto> {
    return this.http.get<StorageRoomLayoutDto>(`${this.base}/rooms/${roomId}/layout`);
  }
  deleteRoom(roomId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/rooms/${roomId}`);
  }

  // ── Paramètres du module ───────────────────────────────
  getStorageSettings(): Observable<StorageSettingsDto> {
    return this.http.get<StorageSettingsDto>(`${this.base}/settings`);
  }
  updateStorageSettings(body: StorageSettingsDto): Observable<StorageSettingsDto> {
    return this.http.put<StorageSettingsDto>(`${this.base}/settings`, body);
  }

  // ── Modèle 3D ──────────────────────────────────────────
  uploadRoomModel(roomId: string, file: File): Observable<StorageCompileJobDto> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<StorageCompileJobDto>(`${this.base}/rooms/${roomId}/model`, form);
  }
  getCompileJob(jobId: string): Observable<StorageCompileJobDto> {
    return this.http.get<StorageCompileJobDto>(`${this.base}/compile-jobs/${jobId}`);
  }

  // ── Cartons ────────────────────────────────────────────
  getBoxes(roomId: string, labelOrigin?: string | null): Observable<StorageBoxDto[]> {
    let params = new HttpParams().set('roomId', roomId);
    if (labelOrigin) params = params.set('labelOrigin', labelOrigin);
    return this.http.get<StorageBoxDto[]>(`${this.base}/boxes`, { params });
  }
  createBox(body: CreateStorageBoxRequest): Observable<StorageBoxDto> {
    return this.http.post<StorageBoxDto>(`${this.base}/boxes`, body);
  }
  updateBoxPosition(boxId: string, position3dJson: string | null): Observable<StorageBoxDto> {
    return this.http.patch<StorageBoxDto>(`${this.base}/boxes/${boxId}/position`, { position3dJson });
  }
  deleteBox(boxId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/boxes/${boxId}`);
  }

  // ── Classeurs ──────────────────────────────────────────
  getBinders(boxId: string): Observable<StorageBinderDto[]> {
    const params = new HttpParams().set('boxId', boxId);
    return this.http.get<StorageBinderDto[]>(`${this.base}/binders`, { params });
  }
  createBinder(body: CreateStorageBinderRequest): Observable<StorageBinderDto> {
    return this.http.post<StorageBinderDto>(`${this.base}/binders`, body);
  }
  placeBinder(
    binderId: string,
    p: { boxId?: string | null; positionInBox?: number | null; furnitureId?: string | null; levelIndex?: number | null },
  ): Observable<StorageBinderDto> {
    return this.http.patch<StorageBinderDto>(`${this.base}/binders/${binderId}/placement`, {
      boxId: p.boxId ?? null, positionInBox: p.positionInBox ?? null,
      furnitureId: p.furnitureId ?? null, levelIndex: p.levelIndex ?? null,
    });
  }

  getBinderCards(binderId: string): Observable<StorageBinderCardDto[]> {
    return this.http.get<StorageBinderCardDto[]>(`${this.base}/binders/${binderId}/cards`);
  }
  getCardDetail(ownerType: string, ownerId: string): Observable<StorageCardDetailDto> {
    const params = new HttpParams().set('ownerType', ownerType).set('ownerId', ownerId);
    return this.http.get<StorageCardDetailDto>(`${this.base}/card-detail`, { params });
  }
  getEstimate(ownerType: 'box' | 'binder', ownerId: string): Observable<StorageEstimateDto> {
    const params = new HttpParams().set('ownerType', ownerType).set('ownerId', ownerId);
    return this.http.get<StorageEstimateDto>(`${this.base}/estimate`, { params });
  }

  // ── Meubles ────────────────────────────────────────────
  createFurniture(body: CreateStorageFurnitureRequest): Observable<StorageFurnitureDto> {
    return this.http.post<StorageFurnitureDto>(`${this.base}/furniture`, body);
  }
  updateFurniturePosition(id: string, position3dJson: string | null): Observable<StorageFurnitureDto> {
    return this.http.patch<StorageFurnitureDto>(`${this.base}/furniture/${id}/position`, { position3dJson });
  }
  deleteFurniture(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/furniture/${id}`);
  }

  // ── Rangement carton (sol OU meuble/niveau) ────────────
  placeBox(
    boxId: string,
    p: { furnitureId?: string | null; levelIndex?: number | null; position3dJson?: string | null },
  ): Observable<StorageBoxDto> {
    return this.http.patch<StorageBoxDto>(`${this.base}/boxes/${boxId}/placement`, {
      furnitureId: p.furnitureId ?? null, levelIndex: p.levelIndex ?? null,
      position3dJson: p.position3dJson ?? null,
    });
  }

  // ── Localisations ──────────────────────────────────────
  assignLocation(
    binderId: string, ownerType: string, ownerId: string, slotPosition: number | null,
  ): Observable<StorageLocationDto> {
    return this.http.post<StorageLocationDto>(`${this.base}/locations`, {
      binderId, ownerType, ownerId, slotPosition,
    });
  }

  /** Assigne plusieurs items du stock boutique à un classeur en un seul appel. */
  batchAssign(binderId: string, ownerIds: string[]): Observable<StorageLocationDto[]> {
    return this.http.post<StorageLocationDto[]>(`${this.base}/binders/${binderId}/assign`, { ownerIds });
  }

  // ── Stock boutique (pour popup assignation) ────────────
  getShopStock(params?: { type?: string; search?: string; page?: number; pageSize?: number }): Observable<PagedResult<ShopStockDto>> {
    let p = new HttpParams();
    if (params?.type) p = p.set('type', params.type);
    if (params?.search) p = p.set('search', params.search);
    if (params?.page != null) p = p.set('page', params.page.toString());
    if (params?.pageSize != null) p = p.set('pageSize', params.pageSize.toString());
    return this.http.get<PagedResult<ShopStockDto>>(`${this.shopBase}/stock`, { params: p });
  }

  // ── Scan classeur double-page ──────────────────────────
  scanBinder(formData: FormData): Observable<ScanBinderResult> {
    return this.http.post<ScanBinderResult>(`${this.base}/scan-binder`, formData);
  }

  createFromScan(binderId: string, ownerType: string, validatedOwnerIds: string[]): Observable<StorageLocationDto[]> {
    return this.http.post<StorageLocationDto[]>(`${this.base}/create-from-scan`, {
      binderId, ownerType, validatedOwnerIds,
    });
  }

  // ── Lookup ─────────────────────────────────────────────
  lookup(code: string): Observable<StorageBoxLookupDto> {
    const params = new HttpParams().set('code', code);
    return this.http.get<StorageBoxLookupDto>(`${this.base}/lookup`, { params });
  }

  // ── Déplacements ───────────────────────────────────────
  getPendingMovements(): Observable<StorageMovementEventDto[]> {
    return this.http.get<StorageMovementEventDto[]>(`${this.base}/movements/pending`);
  }
  confirmMovement(id: string, confirm: boolean): Observable<StorageMovementEventDto> {
    return this.http.post<StorageMovementEventDto>(`${this.base}/movements/${id}/confirm`, { confirm });
  }
}
