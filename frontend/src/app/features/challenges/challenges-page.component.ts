import { Component, computed, inject, signal } from "@angular/core";
import type { Observable } from "rxjs";
import { forkJoin } from "rxjs";
import { AuthStateService } from "../../core/auth/auth-state.service";
import { formatCopaCoin } from "../../shared/copa-coin-format";
import { MatchesService } from "../matches/matches.service";
import type { CurrentUserSummary, MatchListItem } from "../matches/matches.models";
import type { ChallengeSide, ChallengeStatus, MatchChallenge } from "./challenges.models";
import { ChallengesService } from "./challenges.service";

@Component({
	selector: "app-challenges-page",
	template: `
		<section class="space-y-6">
			<header class="overflow-hidden rounded-[2rem] border border-white/70 bg-white/85 p-6 shadow-xl shadow-sky-900/5 backdrop-blur dark:border-slate-700 dark:bg-slate-950/80" data-testid="challenges-header">
				<div class="grid gap-5 sm:grid-cols-[1fr_auto] sm:items-center">
					<div>
						<p class="text-sm font-bold uppercase tracking-[0.24em] text-sky-700 dark:text-sky-300">Custom challenges</p>
						<h1 class="mt-2 text-4xl font-black tracking-tight text-slate-950 dark:text-white">Open a match challenge</h1>
						<p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600 dark:text-slate-300">Create free-text CopaCoin challenges for a match, accept the opposite side, and keep your wallet in sync as stakes move through escrow.</p>
					</div>
				</div>
			</header>

			@if (isLoading()) {
				<section class="rounded-2xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-700 dark:border-sky-900 dark:bg-sky-950 dark:text-sky-200" data-testid="challenges-loading">Loading challenges...</section>
			}

			@if (errorMessage()) {
				<section class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-200" data-testid="challenges-error">{{ errorMessage() }}</section>
			}

			@if (successMessage()) {
				<section class="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-200" data-testid="challenges-success">{{ successMessage() }}</section>
			}

			@if (!isLoading() && userSummary()) {
				<section class="rounded-[2rem] border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80" data-testid="challenge-create-card">
					<div class="mb-4 flex flex-wrap items-center justify-between gap-3 text-sm">
						<p class="font-bold text-slate-950 dark:text-white">{{ userSummary()!.displayName }}</p>
						<div class="flex flex-wrap gap-2 text-xs font-bold">
							@if (pendingChallengeStakeCc() > 0) {
								<span class="rounded-full bg-amber-50 px-3 py-1 text-amber-700 dark:bg-amber-950 dark:text-amber-200">{{ formatCopaCoin(pendingChallengeStakeCc()) }} CC pending</span>
							}
							<span class="rounded-full bg-slate-100 px-3 py-1 text-slate-600 dark:bg-slate-800 dark:text-slate-300">{{ formatCopaCoin(availableBalanceCc()) }} CC available</span>
						</div>
					</div>
					<div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_8rem]">
						<label class="grid gap-2 text-sm font-medium text-slate-700 dark:text-slate-200">
							<span>Match</span>
							<select class="rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-white" [value]="selectedMatchId()" (change)="selectMatch($any($event.target).value)" data-testid="challenge-match-select">
								@for (match of matches(); track match.id) {
									<option [value]="match.id">{{ getMatchLabel(match) }}</option>
								}
							</select>
						</label>
						<label class="grid gap-2 text-sm font-medium text-slate-700 dark:text-slate-200">
							<span>Stake</span>
							<input class="w-full min-w-0 rounded-xl border border-slate-300 bg-white px-3 py-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-white" type="number" min="1" step="1" [value]="stakeAmountCc()" (input)="stakeAmountCc.set(toNumber($any($event.target).value))" data-testid="challenge-stake-input" />
						</label>
					</div>
					<div class="mt-4 grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-end">
						<label class="grid gap-2 text-sm font-medium text-slate-700 dark:text-slate-200">
							<span>Claim</span>
							<textarea class="min-h-24 rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-white" maxlength="280" [value]="claimText()" (input)="claimText.set($any($event.target).value)" placeholder="Example: Mexico wins by 2+ goals." data-testid="challenge-claim-input"></textarea>
						</label>
						<button type="button" class="rounded-xl border border-sky-600 bg-sky-600 px-5 py-3 text-sm font-bold text-white transition hover:bg-sky-700 disabled:cursor-not-allowed disabled:opacity-60" [disabled]="!canCreateChallenge() || isCreating()" (click)="createChallenge()" data-testid="challenge-create-button">{{ isCreating() ? "Creating..." : "Create challenge" }}</button>
					</div>
					@if (selectedMatch() && !selectedMatch()!.isBettingOpen) {
						<p class="mt-3 text-sm font-semibold text-amber-700 dark:text-amber-300">Challenge creation and acceptance close 5 minutes after kickoff.</p>
					}
				</section>

				@if (challenges().length === 0) {
					<section class="grid gap-5 rounded-2xl border border-dashed border-slate-300 bg-white/80 p-8 text-sm text-slate-600 dark:border-slate-700 dark:bg-slate-950/60 dark:text-slate-300 sm:grid-cols-[1fr_auto] sm:items-center" data-testid="challenges-empty">
						<span>No custom challenges exist for this match yet. Create the first challenge and escrow the opening stake.</span>
						<img class="h-28 w-28 object-contain" src="/assets/brand/empty-state-mascot.webp" alt="Empty challenges mascot" />
					</section>
				} @else {
					<section class="grid gap-4" data-testid="challenges-list">
						@for (challenge of challenges(); track challenge.id) {
							<article class="overflow-hidden rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80" [attr.data-testid]="'challenge-card-' + challenge.id">
								<div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
									<div class="min-w-0">
										<div class="flex flex-wrap gap-2 text-xs font-bold uppercase tracking-wide">
											<span class="rounded-full bg-slate-100 px-3 py-1 text-slate-700 dark:bg-slate-800 dark:text-slate-200">{{ getStatusLabel(challenge.status) }}</span>
											<span class="rounded-full bg-amber-50 px-3 py-1 text-amber-700 dark:bg-amber-950 dark:text-amber-200">{{ formatCopaCoin(challenge.stakeAmountCc) }} CC each</span>
											@if (challenge.status === "Settled" && challenge.winnerSide) {
												<span class="rounded-full bg-emerald-50 px-3 py-1 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-200">Winner: {{ getWinnerName(challenge) }}</span>
											}
										</div>
										<h2 class="mt-3 text-xl font-black text-slate-950 dark:text-white">{{ challenge.claimText }}</h2>
										<div class="mt-3 grid gap-2 text-sm text-slate-600 dark:text-slate-300 sm:grid-cols-2">
											<p class="rounded-xl px-3 py-2" [class]="getPositionCardClasses(challenge, 'Creator')"><span class="font-black" [class]="getPositionLabelClasses(challenge, 'Creator')">For the claim</span><br />{{ getPositionName(challenge, "Creator") }}</p>
											<p class="rounded-xl px-3 py-2" [class]="getPositionCardClasses(challenge, 'Taker')"><span class="font-black" [class]="getPositionLabelClasses(challenge, 'Taker')">Against the claim</span><br />{{ getPositionName(challenge, "Taker") }}</p>
										</div>
									</div>
									@if (challenge.status === "Open" && canAccept(challenge)) {
										<button type="button" class="rounded-xl border border-emerald-600 bg-emerald-600 px-4 py-3 text-sm font-black text-white shadow-lg shadow-emerald-900/10 transition hover:-translate-y-0.5 hover:bg-emerald-700 disabled:cursor-not-allowed disabled:translate-y-0 disabled:opacity-60" [disabled]="submittingChallengeId() === challenge.id" (click)="acceptChallenge(challenge)" [attr.data-testid]="'challenge-accept-' + challenge.id">{{ submittingChallengeId() === challenge.id ? "Joining..." : "Take the challenge" }}</button>
									}
									@if (canCancel(challenge)) {
										<button type="button" class="rounded-xl border border-rose-300 px-4 py-3 text-sm font-bold text-rose-700 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-rose-900 dark:text-rose-200 dark:hover:bg-rose-950" [disabled]="submittingChallengeId() === challenge.id" (click)="cancelChallenge(challenge)" [attr.data-testid]="'challenge-cancel-' + challenge.id">{{ submittingChallengeId() === challenge.id ? "Canceling..." : "Cancel" }}</button>
									}
								</div>
								@if (isAdmin() && challenge.status !== "Settled" && challenge.status !== "Voided" && challenge.status !== "Expired") {
									<div class="mt-5 flex flex-wrap gap-3 border-t border-slate-100 pt-5 dark:border-slate-800">
										<button type="button" class="rounded-xl border border-slate-300 px-4 py-2 text-sm font-bold text-slate-700 transition hover:border-sky-400 hover:text-sky-700 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:text-slate-200" [disabled]="challenge.status !== 'Matched' || submittingChallengeId() === challenge.id" (click)="settleChallenge(challenge, 'Creator')">Claim happened</button>
										<button type="button" class="rounded-xl border border-slate-300 px-4 py-2 text-sm font-bold text-slate-700 transition hover:border-sky-400 hover:text-sky-700 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:text-slate-200" [disabled]="challenge.status !== 'Matched' || submittingChallengeId() === challenge.id" (click)="settleChallenge(challenge, 'Taker')">Claim failed</button>
										<button type="button" class="rounded-xl border border-rose-300 px-4 py-2 text-sm font-bold text-rose-700 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-rose-900 dark:text-rose-200 dark:hover:bg-rose-950" [disabled]="submittingChallengeId() === challenge.id" (click)="voidChallenge(challenge)">Void</button>
										<button type="button" class="rounded-xl border border-amber-300 px-4 py-2 text-sm font-bold text-amber-700 transition hover:bg-amber-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-amber-900 dark:text-amber-200 dark:hover:bg-amber-950" [disabled]="submittingChallengeId() === challenge.id" (click)="expireChallenge(challenge)">Expire</button>
									</div>
								}
							</article>
						}
					</section>
				}
			}
		</section>
	`,
})
export class ChallengesPageComponent {
	private readonly authState = inject(AuthStateService);
	private readonly challengesService = inject(ChallengesService);
	private readonly matchesService = inject(MatchesService);
	protected readonly formatCopaCoin = formatCopaCoin;

	readonly userSummary = signal<CurrentUserSummary | null>(null);
	readonly matches = signal<ReadonlyArray<MatchListItem>>([]);
	readonly challenges = signal<ReadonlyArray<MatchChallenge>>([]);
	readonly selectedMatchId = signal<number | null>(null);
	readonly claimText = signal("");
	readonly stakeAmountCc = signal(10);
	readonly isLoading = signal(true);
	readonly isCreating = signal(false);
	readonly submittingChallengeId = signal<number | null>(null);
	readonly errorMessage = signal("");
	readonly successMessage = signal("");

	readonly pendingChallengeStakeCc = computed(() => this.challenges()
		.filter((challenge) => challenge.status === "Open" || challenge.status === "Matched")
		.reduce((total, challenge) => total + this.getCurrentUserStake(challenge), 0));
	readonly availableBalanceCc = computed(() => this.userSummary()?.currentBalanceCc ?? 0);
	readonly selectedMatch = computed(() => this.matches().find((match) => match.id === this.selectedMatchId()) ?? null);

	constructor() {
		this.loadPageData();
	}

	selectMatch(value: string): void {
		const matchId = Number(value);
		if (!Number.isFinite(matchId) || matchId <= 0 || matchId === this.selectedMatchId()) {
			return;
		}

		this.selectedMatchId.set(matchId);
		this.loadChallenges(matchId);
	}

	canCreateChallenge(): boolean {
		return !!this.selectedMatchId() && this.selectedMatch()?.isBettingOpen === true && this.claimText().trim().length > 0 && this.stakeAmountCc() > 0;
	}

	createChallenge(): void {
		const matchId = this.selectedMatchId();
		if (!matchId || !this.canCreateChallenge()) {
			return;
		}

		this.errorMessage.set("");
		this.successMessage.set("");
		this.isCreating.set(true);
		this.challengesService.createChallenge({
			matchId,
			claimText: this.claimText().trim(),
			stakeAmountCc: this.stakeAmountCc(),
		}).subscribe({
			next: (result) => {
				this.upsertChallenge(result.challenge);
				this.updateCurrentBalance(result.currentBalanceCc);
				this.refreshUserSummary("Your challenge was created, but the wallet could not refresh. Reload the page to confirm the latest balance.");
				this.claimText.set("");
				this.successMessage.set(`Challenge created. Remaining balance: ${formatCopaCoin(result.currentBalanceCc)} CC.`);
				this.isCreating.set(false);
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? "Unable to create the challenge right now.");
				this.isCreating.set(false);
			},
		});
	}

	canAccept(challenge: MatchChallenge): boolean {
		return challenge.status === "Open" && this.selectedMatch()?.isBettingOpen === true && !this.isCurrentUserPosition(challenge, "Creator");
	}
	canCancel(challenge: MatchChallenge): boolean {
		return challenge.status === "Open" && this.isCurrentUserPosition(challenge, "Creator");
	}
	acceptChallenge(challenge: MatchChallenge): void {
		this.runChallengeMutation(
			challenge.id,
			() => this.challengesService.acceptChallenge(challenge.id),
			(result) => {
				this.upsertChallenge(result.challenge);
				this.updateCurrentBalance(result.currentBalanceCc);
				this.refreshUserSummary("The challenge was accepted, but the wallet could not refresh. Reload the page to confirm the latest balance.");
				this.successMessage.set(`Challenge accepted. Remaining balance: ${formatCopaCoin(result.currentBalanceCc)} CC.`);
			},
			"Unable to accept the challenge right now.",
		);
	}
	cancelChallenge(challenge: MatchChallenge): void {
		this.runChallengeMutation(
			challenge.id,
			() => this.challengesService.cancelChallenge(challenge.id),
			(result) => {
				this.upsertChallenge(result.challenge);
				this.updateCurrentBalance(result.currentBalanceCc);
				this.refreshUserSummary("The challenge was canceled, but the wallet could not refresh. Reload the page to confirm the latest balance.");
				this.successMessage.set(`Challenge canceled. Current balance: ${formatCopaCoin(result.currentBalanceCc)} CC.`);
			},
			"Unable to cancel the challenge right now.",
		);
	}

	settleChallenge(challenge: MatchChallenge, winnerSide: ChallengeSide): void {
		this.runLifecycleMutation(challenge.id, () => this.challengesService.settleChallenge(challenge.id, { winnerSide }), `${this.getWinnerLabel(winnerSide)} won the challenge.`);
	}
	voidChallenge(challenge: MatchChallenge): void {
		this.runLifecycleMutation(challenge.id, () => this.challengesService.voidChallenge(challenge.id), "Challenge voided and active stakes refunded.");
	}
	expireChallenge(challenge: MatchChallenge): void {
		this.runLifecycleMutation(challenge.id, () => this.challengesService.expireChallenge(challenge.id), "Challenge expired and active stakes refunded.");
	}
	isAdmin(): boolean {
		return this.authState.user()?.roles.includes("Admin") ?? false;
	}
	getMatchLabel(match: MatchListItem): string {
		return `${match.homeTeamName} vs ${match.awayTeamName}`;
	}
	getPositionName(challenge: MatchChallenge, side: ChallengeSide): string {
		return challenge.positions.find((position) => position.side === side)?.displayName || "Waiting for challenger";
	}
	getStatusLabel(status: ChallengeStatus): string {
		return {
			Open: "Open",
			Matched: "Matched",
			Settled: "Settled",
			Voided: "Voided",
			Expired: "Expired",
		}[status];
	}
	getWinnerLabel(side: ChallengeSide): string {
		return side === "Creator" ? "Claim" : "Challenge taker";
	}
	getWinnerName(challenge: MatchChallenge): string {
		return challenge.winnerSide ? this.getPositionName(challenge, challenge.winnerSide) : "Pending";
	}
	getPositionCardClasses(challenge: MatchChallenge, side: ChallengeSide): string {
		if (challenge.status === "Settled" && challenge.winnerSide === side) {
			return "border border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-800 dark:bg-emerald-950 dark:text-emerald-200";
		}

		return "bg-slate-50 dark:bg-slate-900";
	}
	getPositionLabelClasses(challenge: MatchChallenge, side: ChallengeSide): string {
		if (challenge.status === "Settled" && challenge.winnerSide === side) {
			return "text-emerald-900 dark:text-emerald-100";
		}

		return "text-slate-800 dark:text-slate-100";
	}
	toNumber(value: string): number {
		const amount = Number(value);
		return Number.isFinite(amount) ? amount : 0;
	}
	private loadPageData(): void {
		forkJoin({
			userSummary: this.matchesService.getCurrentUserSummary(),
			matches: this.matchesService.listMatches(),
		}).subscribe({
			next: ({ userSummary, matches }) => {
				this.userSummary.set(userSummary);
				this.matches.set(matches);
				const initialMatchId = matches[0]?.id ?? null;
				this.selectedMatchId.set(initialMatchId);
				if (initialMatchId) {
					this.loadChallenges(initialMatchId);
				} else {
					this.isLoading.set(false);
				}
			},
			error: () => {
				this.errorMessage.set("Unable to load challenges right now.");
				this.isLoading.set(false);
			},
		});
	}
	private loadChallenges(matchId: number): void {
		this.errorMessage.set("");
		this.isLoading.set(true);
		this.challengesService.listChallenges(matchId).subscribe({
			next: (challenges) => {
				this.challenges.set(challenges);
				this.isLoading.set(false);
			},
			error: () => {
				this.errorMessage.set("Unable to load challenges for this match.");
				this.isLoading.set(false);
			},
		});
	}
	private runLifecycleMutation(challengeId: number, request: () => Observable<MatchChallenge>, successMessage: string): void {
		this.runChallengeMutation(challengeId, request, (challenge) => {
			this.upsertChallenge(challenge);
			this.refreshUserSummary("The challenge action succeeded, but the wallet could not refresh. Reload the page to confirm the latest balance.");
			this.successMessage.set(successMessage);
		}, "Unable to update the challenge right now.");
	}
	private runChallengeMutation<T>(challengeId: number, request: () => Observable<T>, onSuccess: (result: T) => void, fallbackError: string): void {
		this.errorMessage.set("");
		this.successMessage.set("");
		this.submittingChallengeId.set(challengeId);
		request().subscribe({
			next: (result) => {
				onSuccess(result);
				this.submittingChallengeId.set(null);
			},
			error: (error) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? fallbackError);
				this.submittingChallengeId.set(null);
			},
		});
	}
	private refreshUserSummary(walletRefreshError: string): void {
		this.matchesService.getCurrentUserSummary().subscribe({
			next: (userSummary) => this.userSummary.set(userSummary),
			error: () => this.errorMessage.set(walletRefreshError),
		});
	}
	private upsertChallenge(challenge: MatchChallenge): void {
		const existing = this.challenges().some((current) => current.id === challenge.id);
		this.challenges.set(existing
			? this.challenges().map((current) => current.id === challenge.id ? challenge : current)
			: [challenge, ...this.challenges()]);
	}
	private updateCurrentBalance(currentBalanceCc: number): void {
		const userSummary = this.userSummary();
		if (userSummary) {
			this.userSummary.set({ ...userSummary, currentBalanceCc });
		}
	}
	private getCurrentUserStake(challenge: MatchChallenge): number {
		const userId = this.userSummary()?.id;
		return challenge.positions.find((position) => position.userId === userId)?.stakeAmountCc ?? 0;
	}
	private isCurrentUserPosition(challenge: MatchChallenge, side: ChallengeSide): boolean {
		const userId = this.userSummary()?.id;
		return challenge.positions.some((position) => position.userId === userId && position.side === side);
	}
}
