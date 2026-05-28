import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthStateService } from './auth-state.service';

export const authTokenInterceptor: HttpInterceptorFn = (request, next) => {
  const authState = inject(AuthStateService);
  const accessToken = authState.accessToken();

  if (!accessToken) {
    return next(request);
  }

  return next(request.clone({
    setHeaders: {
      Authorization: `Bearer ${accessToken}`
    }
  }));
};
