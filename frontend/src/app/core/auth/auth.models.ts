export interface AuthenticatedUser {
	id: number;
	email: string;
	displayName: string;
	roles: ReadonlyArray<string>;
}

export interface AuthResponse {
	accessToken: string;
	user: AuthenticatedUser;
}

export interface StoredAuthState {
	accessToken: string;
}
