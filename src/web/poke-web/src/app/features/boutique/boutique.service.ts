import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '@env/environment';
import {
  ShopDto, UpdateShopInfoRequest,
  ShopProductDto, ShopProductRequest, ProductFilters,
  ShopStockDto, ShopStockRequest, StockFilters,
  ShopAppointmentDto, UpdateAppointmentRequest, AppointmentFilters,
  AppointmentStatus, PagedResult, CardSummary,
  PlvCompileJobDto, ShopPlvModel,
} from './boutique.models';

@Injectable({ providedIn: 'root' })
export class BoutiqueService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/v1/admin/shop`;

  // ── Infos ──────────────────────────────────────────────
  getInfo(): Observable<ShopDto> {
    return this.http.get<ShopDto>(`${this.base}/info`);
  }
  updateInfo(body: UpdateShopInfoRequest): Observable<ShopDto> {
    return this.http.put<ShopDto>(`${this.base}/info`, body);
  }

  // ── PLV Présentoir (modèle 3D .blend → .glb) ───────────
  getPlvModel(): Observable<ShopPlvModel> {
    return this.http.get<ShopPlvModel>(`${this.base}/plv/model`);
  }
  uploadPlvModel(file: File): Observable<PlvCompileJobDto> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<PlvCompileJobDto>(`${this.base}/plv/model`, form);
  }
  getPlvCompileJob(jobId: string): Observable<PlvCompileJobDto> {
    return this.http.get<PlvCompileJobDto>(`${this.base}/plv/compile-jobs/${jobId}`);
  }

  // ── Présentoir ─────────────────────────────────────────
  getProducts(f: ProductFilters): Observable<PagedResult<ShopProductDto>> {
    let params = new HttpParams().set('page', f.page).set('pageSize', f.pageSize);
    if (f.type) params = params.set('type', f.type);
    if (f.isAvailable != null) params = params.set('isAvailable', f.isAvailable);
    if (f.search) params = params.set('search', f.search);
    return this.http.get<PagedResult<ShopProductDto>>(`${this.base}/products`, { params });
  }
  createProduct(body: ShopProductRequest): Observable<ShopProductDto> {
    return this.http.post<ShopProductDto>(`${this.base}/products`, body);
  }
  updateProduct(id: string, body: ShopProductRequest): Observable<ShopProductDto> {
    return this.http.put<ShopProductDto>(`${this.base}/products/${id}`, body);
  }
  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/products/${id}`);
  }
  toggleAvailability(id: string, isAvailable: boolean): Observable<ShopProductDto> {
    return this.http.patch<ShopProductDto>(`${this.base}/products/${id}/availability`, { isAvailable });
  }

  // ── Stock ──────────────────────────────────────────────
  getStock(f: StockFilters): Observable<PagedResult<ShopStockDto>> {
    let params = new HttpParams().set('page', f.page).set('pageSize', f.pageSize);
    if (f.type) params = params.set('type', f.type);
    if (f.search) params = params.set('search', f.search);
    return this.http.get<PagedResult<ShopStockDto>>(`${this.base}/stock`, { params });
  }
  createStock(body: ShopStockRequest): Observable<ShopStockDto> {
    return this.http.post<ShopStockDto>(`${this.base}/stock`, body);
  }
  updateStock(id: string, body: ShopStockRequest): Observable<ShopStockDto> {
    return this.http.put<ShopStockDto>(`${this.base}/stock/${id}`, body);
  }
  deleteStock(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/stock/${id}`);
  }

  // ── Planning RDV ───────────────────────────────────────
  getAppointments(f: AppointmentFilters): Observable<ShopAppointmentDto[]> {
    let params = new HttpParams().set('page', 1).set('pageSize', 500);
    if (f.status) params = params.set('status', f.status);
    if (f.serviceType) params = params.set('serviceType', f.serviceType);
    if (f.from) params = params.set('from', f.from);
    if (f.to) params = params.set('to', f.to);
    return this.http
      .get<PagedResult<ShopAppointmentDto>>(`${this.base}/appointments`, { params })
      .pipe(map(r => r.items));
  }
  updateAppointmentStatus(id: string, status: AppointmentStatus): Observable<ShopAppointmentDto> {
    return this.http.put<ShopAppointmentDto>(`${this.base}/appointments/${id}/status`, { status });
  }
  updateAppointment(id: string, body: UpdateAppointmentRequest): Observable<ShopAppointmentDto> {
    return this.http.put<ShopAppointmentDto>(`${this.base}/appointments/${id}`, body);
  }

  // ── Autocomplete cartes (réutilise l'endpoint public pokemon/cards) ──
  searchCards(search: string): Observable<CardSummary[]> {
    const params = new HttpParams().set('search', search).set('page', 1).set('pageSize', 10);
    return this.http
      .get<PagedResult<{ id: string; name: string; imageSmall: string | null; setName: string }>>(
        `${environment.apiBaseUrl}/v1/pokemon/cards`, { params })
      .pipe(map(r => r.items.map(c => ({
        id: c.id, name: c.name, imageSmall: c.imageSmall, setName: c.setName,
      }))));
  }
}
