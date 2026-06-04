import { HttpErrorResponse, type HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { Router } from "@angular/router";
import { catchError, throwError } from "rxjs";
import { AuthStateService } from "./auth-state.service";

export const authTokenInterceptor: HttpInterceptorFn = (request, next) => {
	const authState = inject(AuthStateService);
	const router = inject(Router);
	const accessToken = authState.accessToken();
	const authenticatedRequest = accessToken
		? request.clone({
				setHeaders: {
					Authorization: `Bearer ${accessToken}`,
				},
			})
		: request;

	return next(authenticatedRequest).pipe(
		catchError((error: unknown) => {
			if (error instanceof HttpErrorResponse && error.status === 401 && accessToken) {
				authState.clear();
				void router.navigateByUrl("/auth/login");
			}

			return throwError(() => error);
		}),
	);
};
