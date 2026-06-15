import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RoleDto, PermissionDto, CreateRoleRequest, UpdateRoleRequest } from './roles.models';

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly http = inject(HttpClient);

  listRoles(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>('/api/v1/roles');
  }

  getRole(id: string): Observable<RoleDto> {
    return this.http.get<RoleDto>(`/api/v1/roles/${id}`);
  }

  createRole(body: CreateRoleRequest): Observable<RoleDto> {
    return this.http.post<RoleDto>('/api/v1/roles', body);
  }

  updateRole(id: string, body: UpdateRoleRequest): Observable<RoleDto> {
    return this.http.put<RoleDto>(`/api/v1/roles/${id}`, body);
  }

  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/roles/${id}`);
  }

  listPermissions(): Observable<PermissionDto[]> {
    return this.http.get<PermissionDto[]>('/api/v1/permissions');
  }
}
