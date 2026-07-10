import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LoginRequest, LoginResponse, RegisterRequest } from './auth.models';
import { environment } from '@env/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  login(req: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/v1/auth/login`, req);
  }

  register(req: RegisterRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/v1/auth/register`, req);
  }

  refresh(refreshToken: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/v1/auth/refresh`, { refreshToken });
  }

  logout(refreshToken: string): Observable<void> {
    return this.http.post<void>(`${this.base}/v1/auth/logout`, { refreshToken });
  }

  me(): Observable<{ id: string; email: string; firstName: string; lastName: string; tenantId: string; roles: string[]; permissions: string[] }> {
    return this.http.get<{ id: string; email: string; firstName: string; lastName: string; tenantId: string; roles: string[]; permissions: string[] }>(`${this.base}/v1/me`);
  }
}
