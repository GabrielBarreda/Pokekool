import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  MarketCardDto, PagedResult, PokemonSetDto, MarketFilters, SetFiltersDto,
  SyncCatalogResultDto, SyncPricesResultDto, SyncPopulationResultDto,
} from './market.models';

@Injectable({ providedIn: 'root' })
export class MarketService {
  private readonly http = inject(HttpClient);

  getSets(): Observable<PokemonSetDto[]> {
    return this.http.get<PokemonSetDto[]>('/api/v1/pokemon/sets');
  }

  getSetFilters(setId: string): Observable<SetFiltersDto> {
    return this.http.get<SetFiltersDto>(`/api/v1/market/sets/${setId}/filters`);
  }

  getMarketCards(setId: string, filters: MarketFilters): Observable<PagedResult<MarketCardDto>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);

    if (filters.search) params = params.set('search', filters.search);
    if (filters.rarity) params = params.set('rarity', filters.rarity);
    if (filters.supertype) params = params.set('supertype', filters.supertype);

    return this.http.get<PagedResult<MarketCardDto>>(
      `/api/v1/market/sets/${setId}/cards`,
      { params }
    );
  }

  syncCatalog(forceRefresh = false): Observable<SyncCatalogResultDto> {
    return this.http.post<SyncCatalogResultDto>(
      `/api/v1/admin/sync/catalog?forceRefresh=${forceRefresh}`,
      {}
    );
  }

  syncPrices(cardIds: string[]): Observable<SyncPricesResultDto> {
    return this.http.post<SyncPricesResultDto>(
      '/api/v1/admin/sync/prices',
      { cardIds }
    );
  }

  syncPopulation(cardIds: string[]): Observable<SyncPopulationResultDto> {
    return this.http.post<SyncPopulationResultDto>(
      '/api/v1/admin/sync/population',
      { cardIds }
    );
  }
}
