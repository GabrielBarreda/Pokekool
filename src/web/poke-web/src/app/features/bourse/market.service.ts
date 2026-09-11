import { Injectable, inject, NgZone } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  MarketCardDto, PagedResult, PokemonSetDto, MarketFilters, SetFiltersDto,
  SyncCatalogResultDto, SyncPricesResultDto, SyncPopulationResultDto,
  CardGradingApiDto, SyncScheduleDto, SyncProgressEvent,
} from './market.models';

@Injectable({ providedIn: 'root' })
export class MarketService {
  private readonly http = inject(HttpClient);
  private readonly zone = inject(NgZone);

  getSets(): Observable<PokemonSetDto[]> {
    return this.http.get<PokemonSetDto[]>('/api/v1/pokemon/sets');
  }

  getSetFilters(setId: string): Observable<SetFiltersDto> {
    return this.http.get<SetFiltersDto>(`/api/v1/market/sets/${setId}/filters`);
  }

  /** setId null = mode "Tous les sets" : catalogue entier, paginé côté serveur. */
  getMarketCards(setId: string | null, filters: MarketFilters): Observable<PagedResult<MarketCardDto>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);

    if (filters.search) params = params.set('search', filters.search);
    if (filters.rarity) params = params.set('rarity', filters.rarity);
    if (filters.supertype) params = params.set('supertype', filters.supertype);

    const url = setId
      ? `/api/v1/market/sets/${setId}/cards`
      : `/api/v1/market/cards`;

    return this.http.get<PagedResult<MarketCardDto>>(url, { params });
  }

  syncCatalog(forceRefresh = false, setTcgId?: string): Observable<SyncCatalogResultDto> {
    let params = new HttpParams().set('forceRefresh', forceRefresh);
    if (setTcgId) params = params.set('setTcgId', setTcgId);
    return this.http.post<SyncCatalogResultDto>('/api/v1/admin/sync/catalog', {}, { params });
  }

  syncPrices(opts: { cardIds?: string[]; setId?: string; stalenessHours?: number } = {}): Observable<SyncPricesResultDto> {
    return this.http.post<SyncPricesResultDto>('/api/v1/admin/sync/prices', {
      cardIds: opts.cardIds ?? null,
      setId:   opts.setId   ?? null,
      stalenessHours: opts.stalenessHours ?? 24,
    });
  }

  getCardGrading(cardId: string): Observable<CardGradingApiDto[]> {
    return this.http.get<CardGradingApiDto[]>(`/api/v1/market/cards/${cardId}/grading`);
  }

  syncPopulation(cardIds: string[]): Observable<SyncPopulationResultDto> {
    return this.http.post<SyncPopulationResultDto>(
      '/api/v1/admin/sync/population',
      { cardIds }
    );
  }

  // ── Sync planifié ──────────────────────────────────────────────────────────

  getSchedule(): Observable<SyncScheduleDto> {
    return this.http.get<SyncScheduleDto>('/api/v1/admin/sync/schedule');
  }

  scheduleSync(time: string, isRecurring: boolean): Observable<SyncScheduleDto> {
    return this.http.post<SyncScheduleDto>('/api/v1/admin/sync/schedule', { time, isRecurring });
  }

  cancelSchedule(): Observable<SyncScheduleDto> {
    return this.http.delete<SyncScheduleDto>('/api/v1/admin/sync/schedule');
  }

  // ── SSE progress ───────────────────────────────────────────────────────────

  /** Ouvre une connexion SSE et retourne un Observable d'événements de progression. */
  connectProgressStream(): Observable<SyncProgressEvent> {
    return new Observable<SyncProgressEvent>(observer => {
      const source = new EventSource('/api/v1/admin/sync/progress');

      source.onmessage = (e) => {
        try {
          const evt: SyncProgressEvent = JSON.parse(e.data);
          this.zone.run(() => observer.next(evt));
        } catch { /* ignore malformed */ }
      };

      source.onerror = () => {
        // L'EventSource se reconnecte automatiquement — on laisse faire
      };

      return () => source.close();
    });
  }
}
