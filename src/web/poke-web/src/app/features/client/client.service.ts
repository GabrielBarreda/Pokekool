import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  AvailableSlotDto,
  CollectionValueDto,
  CreateAppointmentBody,
  PagedResult,
  ShopAppointmentDto,
  ShopProductDto,
  ShopProductFilters,
} from './client.models';

@Injectable({ providedIn: 'root' })
export class ClientService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl; // "/api"

  // ── Collection ──────────────────────────────────────────────
  getCollectionValue(): Observable<CollectionValueDto> {
    return this.http.get<CollectionValueDto>(`${this.base}/v1/collection/value`);
  }

  // ── Boutique ────────────────────────────────────────────────
  getProducts(filters: ShopProductFilters): Observable<PagedResult<ShopProductDto>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);

    if (filters.type) params = params.set('type', filters.type);
    if (filters.gradeMin != null) params = params.set('grade_min', filters.gradeMin);
    if (filters.gradeMax != null) params = params.set('grade_max', filters.gradeMax);
    if (filters.priceMax != null) params = params.set('price_max', filters.priceMax);
    if (filters.search) params = params.set('search', filters.search);

    return this.http.get<PagedResult<ShopProductDto>>(`${this.base}/v1/shop/products`, { params });
  }

  requestPurchase(productId: string, message: string | null): Observable<string> {
    return this.http.post<string>(
      `${this.base}/v1/shop/products/${productId}/request-purchase`,
      { message },
    );
  }

  // ── RDV ─────────────────────────────────────────────────────
  getAvailableSlots(serviceType: string, date: string): Observable<AvailableSlotDto[]> {
    const params = new HttpParams().set('service_type', serviceType).set('date', date);
    return this.http.get<AvailableSlotDto[]>(`${this.base}/v1/shop/appointments/available-slots`, { params });
  }

  createAppointment(body: CreateAppointmentBody): Observable<ShopAppointmentDto> {
    return this.http.post<ShopAppointmentDto>(`${this.base}/v1/shop/appointments`, body);
  }

  getMyAppointments(page = 1, pageSize = 20): Observable<PagedResult<ShopAppointmentDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<ShopAppointmentDto>>(`${this.base}/v1/shop/appointments/mine`, { params });
  }

  cancelAppointment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/v1/shop/appointments/${id}/cancel`);
  }

  // ── Compte ──────────────────────────────────────────────────
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.base}/v1/account/change-password`, { currentPassword, newPassword });
  }

  changeEmail(newEmail: string): Observable<void> {
    return this.http.post<void>(`${this.base}/v1/account/change-email`, { newEmail });
  }

  deleteAccount(): Observable<void> {
    return this.http.delete<void>(`${this.base}/v1/account`);
  }
}
