import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardSummaryDto, SyncScheduleDto, EmbeddingReindexStatusDto } from './dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getSummary(): Observable<DashboardSummaryDto> {
    return this.http.get<DashboardSummaryDto>('/api/v1/dashboard/summary');
  }

  getSyncStatus(): Observable<SyncScheduleDto> {
    return this.http.get<SyncScheduleDto>('/api/v1/admin/sync/schedule');
  }

  getEmbeddingReindexStatus(): Observable<EmbeddingReindexStatusDto> {
    return this.http.get<EmbeddingReindexStatusDto>('/api/v1/admin/cards/reindex-embeddings/status');
  }
}
