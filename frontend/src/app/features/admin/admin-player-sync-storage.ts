const LAST_PLAYER_SYNC_KEY = "worldcupbets.admin.lastPlayerSyncAtUtc";

export function loadLastPlayerSyncAtUtc(): string | null {
	if (typeof localStorage === "undefined") {
		return null;
	}

	return localStorage.getItem(LAST_PLAYER_SYNC_KEY);
}

export function persistLastPlayerSyncAtUtc(syncedAtUtc: string): void {
	if (typeof localStorage === "undefined") {
		return;
	}

	localStorage.setItem(LAST_PLAYER_SYNC_KEY, syncedAtUtc);
}
