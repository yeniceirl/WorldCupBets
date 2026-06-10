import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import type { Observable } from "rxjs";
import type { ChallengeMutationResult, MatchChallenge, CreateChallengeRequest, SettleChallengeRequest } from "./challenges.models";

@Injectable({ providedIn: "root" })
export class ChallengesService {
	private readonly httpClient = inject(HttpClient);
	listChallenges(matchId: number): Observable<ReadonlyArray<MatchChallenge>> {
		return this.httpClient.get<ReadonlyArray<MatchChallenge>>("/api/challenges", {
			params: { matchId },
		});
	}
	createChallenge(request: CreateChallengeRequest): Observable<ChallengeMutationResult> {
		return this.httpClient.post<ChallengeMutationResult>("/api/challenges", request);
	}
	acceptChallenge(challengeId: number): Observable<ChallengeMutationResult> {
		return this.httpClient.post<ChallengeMutationResult>(`/api/challenges/${challengeId}/accept`, null);
	}
	cancelChallenge(challengeId: number): Observable<ChallengeMutationResult> {
		return this.httpClient.post<ChallengeMutationResult>(`/api/challenges/${challengeId}/cancel`, null);
	}
	settleChallenge(challengeId: number, request: SettleChallengeRequest): Observable<MatchChallenge> {
		return this.httpClient.post<MatchChallenge>(`/api/challenges/${challengeId}/settlement`, request);
	}
	voidChallenge(challengeId: number): Observable<MatchChallenge> {
		return this.httpClient.post<MatchChallenge>(`/api/challenges/${challengeId}/void`, null);
	}
	expireChallenge(challengeId: number): Observable<MatchChallenge> {
		return this.httpClient.post<MatchChallenge>(`/api/challenges/${challengeId}/expire`, null);
	}
}
