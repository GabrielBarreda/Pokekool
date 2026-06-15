import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  EstablishmentDto,
  CreateEstablishmentRequest,
  UpdateEstablishmentRequest,
} from './establishment.models';

@Injectable({ providedIn: 'root' })
export class EstablishmentService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/establishments';

  list(includeInactive = false): Observable<EstablishmentDto[]> {
    const params = new HttpParams().set('includeInactive', includeInactive);
    return this.http.get<EstablishmentDto[]>(this.base, { params });
  }

  get(id: string): Observable<EstablishmentDto> {
    return this.http.get<EstablishmentDto>(`${this.base}/${id}`);
  }

  create(body: CreateEstablishmentRequest): Observable<EstablishmentDto> {
    return this.http.post<EstablishmentDto>(this.base, body);
  }

  update(id: string, body: UpdateEstablishmentRequest): Observable<EstablishmentDto> {
    return this.http.put<EstablishmentDto>(`${this.base}/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
