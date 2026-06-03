import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import type { Observable } from "rxjs";
import type { LeaderboardItem } from "./leaderboard.models";

@Injectable({ providedIn: "root" })
export class LeaderboardService {
	private readonly httpClient = inject(HttpClient);

	getLeaderboard(): Observable<ReadonlyArray<LeaderboardItem>> {
		return this.httpClient.get<ReadonlyArray<LeaderboardItem>>("/api/leaderboard");
	}
}
