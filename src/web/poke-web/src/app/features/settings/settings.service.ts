import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProviderSettingsDto, TestConnectionResultDto } from './settings.models';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(HttpClient);

  getSettings(provider: string): Observable<ProviderSettingsDto> {
    return this.http.get<ProviderSettingsDto>(`/api/v1/admin/providers/${provider}/settings`);
  }

  upsertSetting(provider: string, key: string, value: string): Observable<void> {
    return this.http.put<void>(`/api/v1/admin/providers/${provider}/settings/${key}`, { value });
  }

  testConnection(provider: string): Observable<TestConnectionResultDto> {
    return this.http.post<TestConnectionResultDto>(
      `/api/v1/admin/providers/${provider}/test-connection`,
      {}
    );
  }
}
