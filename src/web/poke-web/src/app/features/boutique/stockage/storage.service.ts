import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  StorageRoomDto, StorageRoomLayoutDto, StorageBoxDto, StorageBinderDto,
  StorageLocationDto, StorageBoxLookupDto, StorageMovementEventDto, StorageCompileJobDto,
  StorageFurnitureDto,
  CreateStorageRoomRequest, CreateStorageBoxRequest, CreateStorageBinderRequest,
  CreateStorageFurnitureRequest,
} from './storage.models';

@Injectable({ providedIn: 'root' })
export class StorageService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/v1/admin/storage`;

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
