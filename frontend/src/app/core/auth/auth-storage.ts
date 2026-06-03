import type { StoredAuthState } from "./auth.models";

const ACCESS_TOKEN_KEY = "worldcupbets.auth.accessToken";
const USER_KEY = "worldcupbets.auth.user";

export function loadStoredAuthState(): StoredAuthState | null {
	if (typeof localStorage === "undefined") {
		return null;
	}

	const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
	const userJson = localStorage.getItem(USER_KEY);
	if (!accessToken || !userJson) {
		return null;
	}

	try {
		return { accessToken, user: JSON.parse(userJson) };
	} catch {
		clearStoredAuthState();
		return null;
	}
}

export function persistAuthState(state: StoredAuthState): void {
	localStorage.setItem(ACCESS_TOKEN_KEY, state.accessToken);
	localStorage.setItem(USER_KEY, JSON.stringify(state.user));
}

export function clearStoredAuthState(): void {
	localStorage.removeItem(ACCESS_TOKEN_KEY);
	localStorage.removeItem(USER_KEY);
}
