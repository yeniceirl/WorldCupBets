import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { forkJoin } from "rxjs";
import { MatchesService } from "../matches/matches.service";
import type { ChampionBetMarket, CurrentUserSummary, MatchBetSelection, MatchListItem } from "../matches/matches.models";

@Component({
	selector: "app-bets-page",
	imports: [DatePipe],
	template: `
		<section class="space-y-6">
			<header class="overflow-hidden rounded-[2rem] border border-white/70 bg-white/85 p-6 shadow-xl shadow-sky-900/5 backdrop-blur dark:border-slate-700 dark:bg-slate-950/80">
				<div class="grid gap-5 sm:grid-cols-[1fr_auto] sm:items-center">
					<div>
						<p class="text-sm font-bold uppercase tracking-[0.24em] text-sky-700 dark:text-sky-300">My Bets</p>
						<h1 class="mt-2 text-4xl font-black tracking-tight text-slate-950 dark:text-white">Your CopaCoin ticket book</h1>
						<p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600 dark:text-slate-300">
							This page is your personal betting ledger: active picks, champion selection, settled results, and wallet context.
						</p>
					</div>
					<img class="mx-auto h-32 w-32 object-contain drop-shadow-xl sm:mx-0" src="/assets/brand/leaderboard-mascot.webp" alt="CopaCoin ticket book mascot" />
				</div>
			</header>

			@if (isLoading()) {
				<section class="rounded-2xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-700 dark:border-sky-900 dark:bg-sky-950 dark:text-sky-200">Loading your bets...</section>
			}

			@if (errorMessage()) {
				<section class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-200">{{ errorMessage() }}</section>
			}

			@if (successMessage()) {
				<section class="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-200">{{ successMessage() }}</section>
			}

			@if (!isLoading() && userSummary() && championMarket()) {
				<section class="grid gap-4 md:grid-cols-3">
					<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
						<p class="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">Balance</p>
						<p class="mt-2 text-3xl font-black text-slate-950 dark:text-white">{{ userSummary()!.currentBalanceCc }} CC</p>
						<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">{{ userSummary()!.displayName }}</p>
					</article>
					<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
						<p class="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">Match picks</p>
						<p class="mt-2 text-3xl font-black text-slate-950 dark:text-white">{{ placedMatchBets().length }}</p>
						<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">{{ settledPickCount() }} settled</p>
					</article>
					<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
						<p class="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">Champion pick</p>
						<p class="mt-2 text-2xl font-black text-slate-950 dark:text-white">{{ championMarket()!.currentUserChampionTeamName ?? "Pending" }}</p>
						<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">Stake: {{ championMarket()!.stakeAmountCc }} CC</p>
					</article>
				</section>

				@if (placedMatchBets().length === 0) {
					<section class="grid gap-5 rounded-2xl border border-dashed border-slate-300 bg-white/80 p-8 text-sm text-slate-600 dark:border-slate-700 dark:bg-slate-950/60 dark:text-slate-300 sm:grid-cols-[1fr_auto] sm:items-center">
						<span>You have not placed match bets yet. Go to Matches and pick your first winner.</span>
						<img class="h-28 w-28 object-contain" src="/assets/brand/empty-state-mascot.webp" alt="Empty bets mascot" />
					</section>
				} @else {
					<section class="grid gap-4" data-testid="my-bets-list">
						@for (match of placedMatchBets(); track match.id) {
							<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
								<div class="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
									<div>
										<p class="text-xs font-bold uppercase tracking-[0.2em] text-sky-700 dark:text-sky-300">{{ match.stage }}</p>
										<h2 class="mt-2 text-xl font-bold text-slate-950 dark:text-white">{{ match.homeTeamName }} vs {{ match.awayTeamName }}</h2>
										<p class="mt-1 text-sm text-slate-600 dark:text-slate-300">{{ match.startsAtUtc | date: "medium" : "UTC" }} UTC</p>
									</div>
									<div class="flex flex-wrap gap-2 text-sm font-medium">
										<span class="rounded-full bg-sky-50 px-3 py-1 text-sky-700 dark:bg-sky-950 dark:text-sky-200">Pick: {{ getSelectionLabel(match.currentUserBetSelection!, match) }}</span>
										<span class="rounded-full bg-amber-50 px-3 py-1 text-amber-700 dark:bg-amber-950 dark:text-amber-200">Stake: {{ match.stakeAmountCc }} CC</span>
										@if (match.officialResult) {
											<span class="rounded-full bg-indigo-50 px-3 py-1 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-200">Result: {{ getSelectionLabel(match.officialResult, match) }}</span>
										}
										<span class="rounded-full px-3 py-1" [class]="match.isSettled ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-200' : 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200'">{{ match.isSettled ? "Settled" : "Waiting" }}</span>
									</div>
								</div>
								@if (match.isBettingOpen && !match.isSettled) {
									<div class="mt-5 border-t border-slate-100 pt-5 dark:border-slate-800">
										<p class="text-sm font-medium text-slate-800 dark:text-slate-200">Change your pick</p>
										<div class="mt-3 flex flex-wrap gap-3">
											@for (selection of betSelections; track selection) {
												<button type="button" class="rounded-xl border px-4 py-2 text-sm font-medium transition" [class]="getBetButtonClasses(match, selection)" [disabled]="submittingMatchId() === match.id || !match.isBettingOpen || match.currentUserBetSelection === selection" (click)="changeMatchBet(match, selection)">
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
			}
		</section>
	`,
})
export class BetsPageComponent {
	private readonly matchesService = inject(MatchesService);
	readonly betSelections: ReadonlyArray<MatchBetSelection> = ["Home", "Draw", "Away"];

	readonly userSummary = signal<CurrentUserSummary | null>(null);
	readonly championMarket = signal<ChampionBetMarket | null>(null);
	readonly matches = signal<ReadonlyArray<MatchListItem>>([]);
	readonly isLoading = signal(true);
	readonly errorMessage = signal("");
	readonly successMessage = signal("");
	readonly submittingMatchId = signal<number | null>(null);
	readonly placedMatchBets = computed(() => this.matches().filter((match) => !!match.currentUserBetSelection));
	readonly settledPickCount = computed(() => this.placedMatchBets().filter((match) => match.isSettled).length);

	constructor() {
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
				this.errorMessage.set("Unable to load your bets right now.");
				this.isLoading.set(false);
			},
		});
	}

	getSelectionLabel(selection: MatchBetSelection, match: MatchListItem): string {
		return selection === "Home" ? match.homeTeamName : selection === "Away" ? match.awayTeamName : "Draw";
	}

	changeMatchBet(match: MatchListItem, selection: MatchBetSelection): void {
		this.errorMessage.set("");
		this.successMessage.set("");
		this.submittingMatchId.set(match.id);
		this.matchesService.placeMatchBet({ matchId: match.id, selection }).subscribe({
			next: (result) => {
				this.matches.set(this.matches().map((currentMatch) =>
					currentMatch.id === result.matchId
						? { ...currentMatch, currentUserBetSelection: result.selection }
						: currentMatch,
				));
				this.successMessage.set(`Updated pick for ${match.homeTeamName} vs ${match.awayTeamName}.`);
				this.submittingMatchId.set(null);
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? "Unable to update your pick.");
				this.submittingMatchId.set(null);
			},
		});
	}

	getBetButtonClasses(match: MatchListItem, selection: MatchBetSelection): string {
		if (this.submittingMatchId() === match.id) {
			return "border-sky-200 bg-sky-50 text-sky-700 dark:border-sky-900 dark:bg-sky-950 dark:text-sky-200 cursor-not-allowed opacity-60";
		}

		if (match.currentUserBetSelection === selection) {
			return "border-emerald-300 bg-emerald-50 text-emerald-800 dark:border-emerald-800 dark:bg-emerald-950 dark:text-emerald-200 cursor-not-allowed";
		}

		if (!match.isBettingOpen) {
			return "border-slate-200 bg-slate-50 text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400 cursor-not-allowed opacity-60";
		}

		return "border-slate-300 bg-white text-slate-800 hover:border-sky-400 hover:text-sky-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:hover:border-sky-400 dark:hover:text-sky-200";
	}
}
