import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { AuthStore } from '@core/auth/auth.store';
import { ToastService } from '@shared/services/toast.service';

let isRefreshing = false;

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // Skip auth endpoints to avoid refresh loops
      const isAuthReq = req.url.includes('/auth/');

      if (err.status === 401 && !isAuthReq) {
        if (!isRefreshing) {
          isRefreshing = true;
          const rt = authStore.refreshToken();
          if (rt) {
            // Trigger refresh — store handles redirect on failure
            authStore.refresh();
            isRefreshing = false;
          } else {
            isRefreshing = false;
            authStore.logout();
          }
        }
        return throwError(() => err);
      }

      if (err.status === 403) {
        toast.show({ message: 'Permission refusée', type: 'danger' });
      }

      if (err.status >= 500) {
        toast.show({ message: 'Erreur serveur. Veuillez réessayer.', type: 'danger' });
      }

      return throwError(() => err);
    }),
  );
};
