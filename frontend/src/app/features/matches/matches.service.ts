import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import type { Observable } from "rxjs";
import type {
	MatchListItem,
	ChampionBetMarket,
	CurrentUserSummary,
	FootballDataSnapshot,
	PlaceChampionBetRequest,
	PlaceChampionBetResult,
	PlaceMatchBetRequest,
	PlaceMatchBetResult,
	RecordMatchResult,
	RecordMatchResultRequest,
	SettleChampionRequest,
	SettleChampionResult,
	SyncFootballDataResult,
	ImportGroupStageFixturesResult,
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

	getFootballDataSnapshot(): Observable<FootballDataSnapshot> {
		return this.httpClient.get<FootballDataSnapshot>("/api/football-data/snapshot");
	}

	placeMatchBet(request: PlaceMatchBetRequest): Observable<PlaceMatchBetResult> {
		return this.httpClient.post<PlaceMatchBetResult>("/api/bets/matches", request);
	}

	placeChampionBet(request: PlaceChampionBetRequest): Observable<PlaceChampionBetResult> {
		return this.httpClient.post<PlaceChampionBetResult>("/api/bets/champion", request);
	}

	recordMatchResult(matchId: number, request: RecordMatchResultRequest): Observable<RecordMatchResult> {
		return this.httpClient.post<RecordMatchResult>(`/api/matches/${matchId}/result`, request);
	}

	settleChampion(request: SettleChampionRequest): Observable<SettleChampionResult> {
		return this.httpClient.post<SettleChampionResult>("/api/bets/champion/settlement", request);
	}

	syncFootballData(): Observable<SyncFootballDataResult> {
		return this.httpClient.post<SyncFootballDataResult>("/api/football-data/sync", null);
	}

	importGroupStageFixtures(): Observable<ImportGroupStageFixturesResult> {
		return this.httpClient.post<ImportGroupStageFixturesResult>("/api/football-data/fixtures/group-stage/import", null);
	}
}
