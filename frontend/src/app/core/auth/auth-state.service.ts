import { Injectable, computed, signal } from "@angular/core";
import type { AuthResponse, AuthenticatedUser } from "./auth.models";
import {
	clearStoredAuthState,
	loadStoredAuthState,
	persistAuthState,
} from "./auth-storage";

@Injectable({ providedIn: "root" })
export class AuthStateService {
	readonly accessToken = signal<string | null>(null);
	readonly user = signal<AuthenticatedUser | null>(null);
	readonly isAuthenticated = computed(() => !!this.accessToken());

	hydrateFromStorage(): void {
		const storedState = loadStoredAuthState();
		this.accessToken.set(storedState?.accessToken ?? null);
		this.user.set(storedState?.user ?? null);
	}

	setAuthenticatedSession(response: AuthResponse): void {
		this.accessToken.set(response.accessToken);
		this.user.set(response.user);
		persistAuthState({ accessToken: response.accessToken, user: response.user });
	}

	clear(): void {
		this.accessToken.set(null);
		this.user.set(null);
		clearStoredAuthState();
	}
}
