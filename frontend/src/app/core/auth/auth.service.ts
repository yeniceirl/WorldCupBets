import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import type { Observable } from "rxjs";
import { catchError, tap, throwError } from "rxjs";
import type { AuthResponse, DevLoginRequest } from "./auth.models";
import { AuthStateService } from "./auth-state.service";

@Injectable({ providedIn: "root" })
export class AuthService {
	private readonly httpClient = inject(HttpClient);
	private readonly authState = inject(AuthStateService);

	exchangeGoogleToken(idToken: string): Observable<AuthResponse> {
		return this.httpClient
			.post<AuthResponse>("/api/auth/google", { idToken })
			.pipe(
				tap((response) => this.authState.setAuthenticatedSession(response)),
				catchError((error) => {
					this.authState.clear();
					return throwError(() => error);
				}),
			);
	}

	devLogin(request: DevLoginRequest = {}): Observable<AuthResponse> {
		return this.httpClient
			.post<AuthResponse>("/api/auth/dev-login", request)
			.pipe(
				tap((response) => this.authState.setAuthenticatedSession(response)),
				catchError((error) => {
					this.authState.clear();
					return throwError(() => error);
				}),
			);
	}

	signOut(): void {
		this.authState.clear();
	}
}
