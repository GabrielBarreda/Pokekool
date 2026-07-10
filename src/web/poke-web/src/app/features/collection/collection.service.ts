import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  BulkAddResult,
  CardScanMatchDto,
  CollectionCardDto,
  CollectionCardStatusDto,
  CollectionFilters,
  CollectionStatsDto,
  PagedResult,
  ScanCollectionResult,
} from './collection.models';

@Injectable({ providedIn: 'root' })
export class CollectionService {
  private readonly http = inject(HttpClient);

  getCards(filters: CollectionFilters): Observable<PagedResult<CollectionCardDto>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize)
      .set('sortBy', filters.sortBy);

    if (filters.setId) params = params.set('setId', filters.setId);
    if (filters.search) params = params.set('search', filters.search);
    if (filters.rarity) params = params.set('rarity', filters.rarity);
    if (filters.year != null) params = params.set('year', filters.year);
    if (filters.ownedOnly) params = params.set('ownedOnly', true);
    if (filters.favoritesOnly) params = params.set('favoritesOnly', true);

    return this.http.get<PagedResult<CollectionCardDto>>('/api/v1/collection/cards', { params });
  }

  getStats(): Observable<CollectionStatsDto> {
    return this.http.get<CollectionStatsDto>('/api/v1/collection/stats');
  }

  toggleOwned(cardId: string): Observable<CollectionCardStatusDto> {
    return this.http.post<CollectionCardStatusDto>(
      `/api/v1/collection/cards/${cardId}/toggle-owned`,
      {}
    );
  }

  setFavorite(cardId: string, isFavorite: boolean): Observable<CollectionCardStatusDto> {
    return this.http.post<CollectionCardStatusDto>(
      `/api/v1/collection/cards/${cardId}/favorite`,
      { isFavorite }
    );
  }

  setPriority(cardId: string, isPriority: boolean): Observable<CollectionCardStatusDto> {
    return this.http.post<CollectionCardStatusDto>(
      `/api/v1/collection/cards/${cardId}/priority`,
      { isPriority }
    );
  }

  scanImage(formData: FormData): Observable<ScanCollectionResult> {
    return this.http.post<ScanCollectionResult>('/api/v1/collection/scan', formData);
  }

  bulkAddOwned(cardIds: string[], matches: CardScanMatchDto[]): Observable<BulkAddResult> {
    return this.http.post<BulkAddResult>('/api/v1/collection/bulk-own', { cardIds, matches });
  }
}
