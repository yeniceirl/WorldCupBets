import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import type { Observable } from "rxjs";
import type {
	MatchListItem,
	ChampionBetMarket,
	CurrentUserSummary,
	PlaceChampionBetRequest,
	PlaceChampionBetResult,
	PlaceMatchBetRequest,
	PlaceMatchBetResult,
} from "./matches.models";

@Injectable({ providedIn: "root" })
export class MatchesService {
	private readonly httpClient = inject(HttpClient);

	listMatches(): Observable<ReadonlyArray<MatchListItem>> {
		return this.httpClient.get<ReadonlyArray<MatchListItem>>("/api/matches");
	}

	getCurrentUserSummary(): Observable<CurrentUserSummary> {
		return this.httpClient.get<CurrentUserSummary>("/api/me/summary");
	}

	getChampionBetMarket(): Observable<ChampionBetMarket> {
		return this.httpClient.get<ChampionBetMarket>("/api/bets/champion");
	}

	placeMatchBet(request: PlaceMatchBetRequest): Observable<PlaceMatchBetResult> {
		return this.httpClient.post<PlaceMatchBetResult>("/api/bets/matches", request);
	}

	placeChampionBet(request: PlaceChampionBetRequest): Observable<PlaceChampionBetResult> {
		return this.httpClient.post<PlaceChampionBetResult>("/api/bets/champion", request);
	}
}
