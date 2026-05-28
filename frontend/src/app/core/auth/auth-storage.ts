import type { StoredAuthState } from "./auth.models";

const ACCESS_TOKEN_KEY = "worldcupbets.auth.accessToken";

export function loadStoredAuthState(): StoredAuthState | null {
	if (typeof localStorage === "undefined") {
		return null;
	}

	const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
	if (!accessToken) {
		return null;
	}

	return { accessToken };
}

export function persistAuthState(state: StoredAuthState): void {
	localStorage.setItem(ACCESS_TOKEN_KEY, state.accessToken);
}

export function clearStoredAuthState(): void {
	localStorage.removeItem(ACCESS_TOKEN_KEY);
}
