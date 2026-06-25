import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserSummaryDto, PagedResult, UsersFilters } from './users.models';

export interface UserDetailDto {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  phone: string | null;
  isActive: boolean;
  emailConfirmed: boolean;
  lastLoginAt: string | null;
  roleIds: string[];
  roleCodes: string[];
  createdAt: string;
  updatedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  getUsers(filters: UsersFilters): Observable<PagedResult<UserSummaryDto>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize)
      .set('sortBy', filters.sortBy);

    if (filters.search)              params = params.set('search', filters.search);
    if (filters.roleCode)            params = params.set('roleCode', filters.roleCode);
    if (filters.hasPendingMessages != null)
      params = params.set('hasPendingMessages', filters.hasPendingMessages);
    if (filters.recentOnly != null)  params = params.set('recentOnly', filters.recentOnly);

    return this.http.get<PagedResult<UserSummaryDto>>('/api/v1/users', { params });
  }

  getUserById(id: string): Observable<UserDetailDto> {
    return this.http.get<UserDetailDto>(`/api/v1/users/${id}`);
  }

  activateUser(id: string): Observable<void> {
    return this.http.patch<void>(`/api/v1/users/${id}/activate`, {});
  }

  deactivateUser(id: string): Observable<void> {
    return this.http.patch<void>(`/api/v1/users/${id}/deactivate`, {});
  }

  assignRoles(id: string, roleCodes: string[]): Observable<UserDetailDto> {
    return this.http.put<UserDetailDto>(`/api/v1/users/${id}/roles`, { roleCodes });
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/users/${id}`);
  }
}
