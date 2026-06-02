import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { forkJoin } from "rxjs";
import { MatchesService } from "./matches.service";
import type {
	ChampionBetMarket,
	CurrentUserSummary,
	MatchBetSelection,
	MatchListItem,
} from "./matches.models";

@Component({
	selector: "app-matches-page",
	imports: [DatePipe],
	template: `
		<section class="space-y-6">
			<header class="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm" data-testid="matches-header">
				<p class="text-sm font-medium uppercase tracking-wide text-sky-700">Match center</p>
				<h1 class="mt-2 text-3xl font-semibold">Upcoming matches</h1>
				<p class="mt-3 text-sm text-slate-600">
					This schedule already includes CopaCoin stake, automatic closing time, your wallet state, and your current picks.
				</p>
			</header>

			@if (successMessage()) {
				<section class="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700" data-testid="success-message">
					{{ successMessage() }}
				</section>
			}

			@if (isLoading()) {
				<section class="rounded-2xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-700">
					Loading matches...
				</section>
			}

			@if (errorMessage()) {
				<section class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700" data-testid="error-message">
					{{ errorMessage() }}
				</section>
			}

			@if (showDashboard()) {
				<section class="grid gap-4 lg:grid-cols-[1.1fr_1.4fr]">
					<article class="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm" data-testid="wallet-card">
						<p class="text-sm font-medium uppercase tracking-wide text-sky-700">Wallet</p>
						<h2 class="mt-2 text-2xl font-semibold text-slate-900">{{ userSummary()!.currentBalanceCc }} CC</h2>
						<p class="mt-2 text-sm text-slate-600">{{ userSummary()!.displayName }} · {{ userSummary()!.email }}</p>
						<div class="mt-4 grid gap-3 sm:grid-cols-2">
							<div class="rounded-xl bg-slate-50 px-4 py-3">
								<p class="text-xs uppercase tracking-wide text-slate-500">Rescues used</p>
								<p class="mt-1 text-lg font-semibold text-slate-900" data-testid="wallet-rescue-count">{{ userSummary()!.rescueCount }}</p>
							</div>
							<div class="rounded-xl bg-slate-50 px-4 py-3">
								<p class="text-xs uppercase tracking-wide text-slate-500">Rescue debt</p>
								<p class="mt-1 text-lg font-semibold text-slate-900" data-testid="wallet-rescue-debt">{{ userSummary()!.rescueDebtCc }} CC</p>
							</div>
						</div>
					</article>

					<article class="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm" data-testid="champion-market-card">
						<p class="text-sm font-medium uppercase tracking-wide text-sky-700">Champion bet</p>
						<h2 class="mt-2 text-2xl font-semibold text-slate-900">{{ championMarket()!.stakeAmountCc }} CC</h2>
						@if (championMarket()!.currentUserChampionTeamName) {
							<p class="mt-3 inline-flex rounded-full bg-emerald-50 px-3 py-1 text-sm font-medium text-emerald-700" data-testid="champion-current-pick">
								Your champion pick: {{ championMarket()!.currentUserChampionTeamName }}
							</p>
						} @else if (championMarket()!.isBettingOpen) {
							<p class="mt-3 text-sm text-slate-600">
								Champion betting is open
								@if (championMarket()!.bettingClosesAtUtc) {
									<span> until {{ championMarket()!.bettingClosesAtUtc | date: "medium" : "UTC" }} UTC</span>
								}.
							</p>
						} @else {
							<p class="mt-3 text-sm text-slate-600">Champion betting is closed.</p>
						}

						@if (!championMarket()!.currentUserChampionTeamName) {
							<div class="mt-4 flex flex-col gap-3 sm:flex-row">
								<select
									#championTeamSelect
									class="min-w-0 flex-1 rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900"
									(change)="selectedChampionTeamName.set(championTeamSelect.value)"
									[attr.data-testid]="'champion-team-select'"
								>
									<option value="">Select a team</option>
									@for (teamName of championMarket()!.teamOptions; track teamName) {
										<option [value]="teamName">{{ teamName }}</option>
									}
								</select>
								<button
									type="button"
									class="rounded-xl border border-sky-600 bg-sky-600 px-4 py-3 text-sm font-medium text-white transition hover:bg-sky-700 disabled:cursor-not-allowed disabled:opacity-60"
									(click)="placeChampionBet()"
									[disabled]="!selectedChampionTeamName() || !championMarket()!.isBettingOpen || isSubmittingChampionBet()"
									data-testid="place-champion-bet-button"
								>
									Place champion bet
								</button>
							</div>
						}
					</article>
				</section>
			}

			@if (!isLoading() && !errorMessage() && matches().length === 0) {
				<section class="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-sm text-slate-600">
					No matches are available yet.
				</section>
			}

			@if (matches().length > 0) {
				<section class="grid gap-4" data-testid="matches-list">
					@for (match of matches(); track match.id) {
						<article class="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm" [attr.data-testid]="'match-card-' + match.id">
							<div class="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
								<div>
									<p class="text-xs font-semibold uppercase tracking-wide text-sky-700">{{ match.stage }}</p>
									<h2 class="mt-2 text-xl font-semibold text-slate-900">
										{{ match.homeTeamName }} vs {{ match.awayTeamName }}
									</h2>
									<p class="mt-2 text-sm text-slate-600">{{ match.venue }}</p>
									<div class="mt-4 flex flex-wrap gap-2 text-sm">
										<span class="rounded-full bg-sky-50 px-3 py-1 font-medium text-sky-700">
											Stake: {{ match.stakeAmountCc }} CC
										</span>
										@if (match.currentUserBetSelection) {
											<span class="rounded-full bg-emerald-50 px-3 py-1 font-medium text-emerald-700" [attr.data-testid]="'match-current-pick-' + match.id">
												Your pick: {{ getSelectionLabel(match.currentUserBetSelection, match) }}
											</span>
										} @else if (match.isBettingOpen) {
											<span class="rounded-full bg-amber-50 px-3 py-1 font-medium text-amber-700">
												Open until {{ match.bettingClosesAtUtc | date: "short" : "UTC" }} UTC
											</span>
										} @else {
											<span class="rounded-full bg-slate-100 px-3 py-1 font-medium text-slate-700">
												Betting closed
											</span>
										}
									</div>
								</div>
								<div class="rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-700">
									<p class="font-medium">{{ match.startsAtUtc | date: "medium" : "UTC" }}</p>
									<p class="mt-1 text-xs uppercase tracking-wide text-slate-500">UTC kickoff</p>
								</div>
							</div>

							@if (!match.currentUserBetSelection) {
								<div class="mt-5 border-t border-slate-100 pt-5">
									<p class="text-sm font-medium text-slate-800">Choose your winner bet</p>
									<div class="mt-3 flex flex-wrap gap-3">
										@for (selection of betSelections; track selection) {
											<button
												type="button"
												class="rounded-xl border px-4 py-2 text-sm font-medium transition"
												[class]="getBetButtonClasses(match.id, match.isBettingOpen)"
												(click)="placeBet(match.id, selection)"
												[disabled]="isSubmitting(match.id) || !match.isBettingOpen"
												[attr.data-testid]="'place-match-bet-' + match.id + '-' + selection.toLowerCase()"
											>
												{{ getSelectionLabel(selection, match) }}
											</button>
										}
									</div>
								</div>
							}
						</article>
					}
				</section>
			}
		</section>
	`,
})
export class MatchesPageComponent {
	private readonly matchesService = inject(MatchesService);
	readonly betSelections: ReadonlyArray<MatchBetSelection> = ["Home", "Draw", "Away"];

	readonly userSummary = signal<CurrentUserSummary | null>(null);
	readonly championMarket = signal<ChampionBetMarket | null>(null);
	readonly matches = signal<ReadonlyArray<MatchListItem>>([]);
	readonly isLoading = signal(true);
	readonly errorMessage = signal("");
	readonly successMessage = signal("");
	readonly selectedChampionTeamName = signal("");
	readonly isSubmittingChampionBet = signal(false);
	readonly submittingMatchId = signal<number | null>(null);
	readonly showDashboard = computed(() => !this.isLoading() && !!this.userSummary() && !!this.championMarket());

	constructor() {
		this.loadPageData();
	}

	placeBet(matchId: number, selection: MatchBetSelection): void {
		this.errorMessage.set("");
		this.successMessage.set("");
		this.submittingMatchId.set(matchId);

		this.matchesService.placeMatchBet({ matchId, selection }).subscribe({
			next: (result) => {
				this.matches.set(this.matches().map((match) =>
					match.id === result.matchId
						? { ...match, currentUserBetSelection: result.selection }
						: match,
				));
				this.successMessage.set(`Bet placed for ${this.getSelectionLabel(selection)}. Remaining balance: ${result.remainingBalanceCc} CC.`);
				this.refreshUserSummary();
				this.submittingMatchId.set(null);
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(
					error.error?.error ??
					error.error?.detail ??
					"Unable to place the match bet right now.");
				this.submittingMatchId.set(null);
			},
		});
	}

	placeChampionBet(): void {
		if (!this.selectedChampionTeamName()) {
			return;
		}

		this.errorMessage.set("");
		this.successMessage.set("");
		this.isSubmittingChampionBet.set(true);

		this.matchesService.placeChampionBet({ teamName: this.selectedChampionTeamName() }).subscribe({
			next: (result) => {
				if (this.championMarket()) {
					this.championMarket.set({
						...this.championMarket()!,
						currentUserChampionTeamName: result.teamName,
					});
				}

				this.successMessage.set(`Champion bet placed for ${result.teamName}. Remaining balance: ${result.remainingBalanceCc} CC.`);
				this.selectedChampionTeamName.set("");
				this.refreshUserSummary();
				this.isSubmittingChampionBet.set(false);
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(
					error.error?.error ??
					error.error?.detail ??
					"Unable to place the champion bet right now.");
				this.isSubmittingChampionBet.set(false);
			},
		});
	}

	isSubmitting(matchId: number): boolean {
		return this.submittingMatchId() === matchId;
	}

	getSelectionLabel(selection: MatchBetSelection, match?: MatchListItem): string {
		return selection === "Home"
			? (match?.homeTeamName ?? "Home")
			: selection === "Away"
				? (match?.awayTeamName ?? "Away")
				: "Draw";
	}

	getBetButtonClasses(matchId: number, isBettingOpen: boolean): string {
		if (this.isSubmitting(matchId)) {
			return "border-sky-200 bg-sky-50 text-sky-700 cursor-not-allowed opacity-60";
		}

		if (!isBettingOpen) {
			return "border-slate-200 bg-slate-50 text-slate-500 cursor-not-allowed opacity-60";
		}

		return "border-slate-300 bg-white text-slate-800 hover:border-sky-400 hover:text-sky-700";
	}

	private loadPageData(): void {
		forkJoin({
			userSummary: this.matchesService.getCurrentUserSummary(),
			championMarket: this.matchesService.getChampionBetMarket(),
			matches: this.matchesService.listMatches(),
		}).subscribe({
			next: ({ userSummary, championMarket, matches }) => {
				this.userSummary.set(userSummary);
				this.championMarket.set(championMarket);
				this.matches.set(matches);
				this.isLoading.set(false);
			},
			error: () => {
				this.errorMessage.set("Unable to load the betting dashboard right now.");
				this.isLoading.set(false);
			},
		});
	}

	private refreshUserSummary(): void {
		this.matchesService.getCurrentUserSummary().subscribe({
			next: (userSummary) => {
				this.userSummary.set(userSummary);
			},
		});
	}
}
