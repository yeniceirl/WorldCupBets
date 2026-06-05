import { DatePipe, NgTemplateOutlet } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { forkJoin } from "rxjs";
import { formatCopaCoin } from "../../shared/copa-coin-format";
import { MatchesService } from "./matches.service";
import type {
	ChampionBetMarket,
	CurrentUserSummary,
	FootballDataSnapshot,
	FootballGroupStanding,
	FootballTeam,
	MatchBetSelection,
	MatchListItem,
} from "./matches.models";

type MatchDayFilter = "Today" | "Tomorrow" | "All";

interface MatchDateGroup {
	dateKey: string;
	label: string;
	matches: ReadonlyArray<MatchListItem>;
}

@Component({
	selector: "app-matches-page",
	imports: [DatePipe, NgTemplateOutlet],
	template: `
		<section class="space-y-6">
			<header class="overflow-hidden rounded-[2rem] border border-white/70 bg-white/85 p-6 shadow-xl shadow-sky-900/5 backdrop-blur dark:border-slate-700 dark:bg-slate-950/80" data-testid="matches-header">
				<div class="grid gap-5 sm:grid-cols-[1fr_auto] sm:items-center">
					<div>
						<p class="text-sm font-bold uppercase tracking-[0.24em] text-sky-700 dark:text-sky-300">Match center</p>
						<h1 class="mt-2 text-4xl font-black tracking-tight text-slate-950 dark:text-white">Upcoming matches</h1>
						<p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600 dark:text-slate-300">
							This schedule already includes CopaCoin stake, automatic closing time, your wallet state, and your current picks.
						</p>
					</div>
					<img class="mx-auto h-32 w-32 object-contain drop-shadow-xl sm:mx-0" src="/assets/brand/empty-state-mascot.webp" alt="CopaCoin match mascot" />
				</div>
			</header>

			@if (successMessage()) {
				<section class="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-200" data-testid="success-message">
					{{ successMessage() }}
				</section>
			}

			@if (isLoading()) {
				<section class="rounded-2xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-700 dark:border-sky-900 dark:bg-sky-950 dark:text-sky-200">
					Loading matches...
				</section>
			}

			@if (errorMessage()) {
				<section class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-200" data-testid="error-message">
					{{ errorMessage() }}
				</section>
			}

			@if (showDashboard()) {
				<section class="grid gap-4 lg:grid-cols-[1.1fr_1.4fr]">
					<article class="rounded-2xl border border-slate-200 bg-white/90 p-6 shadow-sm dark:border-slate-700 dark:bg-slate-950/80" data-testid="wallet-card">
						<p class="text-sm font-bold uppercase tracking-wide text-sky-700 dark:text-sky-300">Total settled</p>
						<h2 class="mt-2 text-2xl font-black text-slate-950 dark:text-white">{{ formatCopaCoin(realizedBalanceCc()) }} CC</h2>
						<div class="mt-2 flex flex-wrap gap-2 text-xs font-bold">
							@if (pendingStakeAmountCc() > 0) {
								<p class="rounded-full bg-amber-50 px-3 py-1 text-amber-700 dark:bg-amber-950 dark:text-amber-200">{{ formatCopaCoin(pendingStakeAmountCc()) }} CC pending</p>
							}
							<p class="rounded-full bg-slate-100 px-3 py-1 text-slate-600 dark:bg-slate-800 dark:text-slate-300">{{ formatCopaCoin(availableBalanceCc()) }} CC available</p>
						</div>
						<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">{{ userSummary()!.displayName }} · {{ userSummary()!.email }}</p>
						<div class="mt-4 grid gap-3 sm:grid-cols-2">
							<div class="rounded-xl bg-slate-50 px-4 py-3 dark:bg-slate-900">
								<p class="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">Rescues used</p>
								<p class="mt-1 text-lg font-semibold text-slate-950 dark:text-white" data-testid="wallet-rescue-count">{{ userSummary()!.rescueCount }}</p>
							</div>
							<div class="rounded-xl bg-slate-50 px-4 py-3 dark:bg-slate-900">
								<p class="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">Rescue debt</p>
								<p class="mt-1 text-lg font-semibold text-slate-950 dark:text-white" data-testid="wallet-rescue-debt">{{ formatCopaCoin(userSummary()!.rescueDebtCc) }} CC</p>
							</div>
						</div>
					</article>

					<article class="rounded-2xl border border-slate-200 bg-white/90 p-6 shadow-sm dark:border-slate-700 dark:bg-slate-950/80" data-testid="champion-market-card">
						<p class="text-sm font-bold uppercase tracking-wide text-sky-700 dark:text-sky-300">Champion bet</p>
						<h2 class="mt-2 text-2xl font-black text-slate-950 dark:text-white">{{ formatCopaCoin(championMarket()!.stakeAmountCc) }} CC</h2>
						@if (championMarket()!.currentUserChampionTeamName) {
							<p class="mt-3 inline-flex rounded-full bg-emerald-50 px-3 py-1 text-sm font-medium text-emerald-700" data-testid="champion-current-pick">
								Your champion pick: {{ championMarket()!.currentUserChampionTeamName }}
							</p>
						} @else if (championMarket()!.isBettingOpen) {
							<p class="mt-3 text-sm text-slate-600 dark:text-slate-300">
								Champion betting is open
								@if (championMarket()!.bettingClosesAtUtc) {
									<span> until {{ championMarket()!.bettingClosesAtUtc | date: "medium" : "UTC" }} UTC</span>
								}.
							</p>
						} @else {
							<p class="mt-3 text-sm text-slate-600 dark:text-slate-300">Champion betting is closed.</p>
						}

						@if (!championMarket()!.currentUserChampionTeamName) {
							<div class="mt-4 flex flex-col gap-3 sm:flex-row">
								<select
									#championTeamSelect
									class="min-w-0 flex-1 rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-white"
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
				<section class="grid gap-5 rounded-2xl border border-dashed border-slate-300 bg-white/80 p-8 text-sm text-slate-600 dark:border-slate-700 dark:bg-slate-950/60 dark:text-slate-300 sm:grid-cols-[1fr_auto] sm:items-center">
					<span>No matches are available yet.</span>
					<img class="h-28 w-28 object-contain" src="/assets/brand/empty-state-mascot.webp" alt="Empty matches mascot" />
				</section>
			}

			@if (matches().length > 0) {
				<section class="rounded-[2rem] border border-slate-200 bg-white/80 p-3 shadow-sm backdrop-blur dark:border-slate-700 dark:bg-slate-950/70" data-testid="match-day-filter">
					<div class="grid gap-2 sm:grid-cols-3">
						@for (filter of matchFilters; track filter) {
							<button type="button" class="rounded-2xl px-4 py-3 text-sm font-black transition" [class]="getMatchFilterClasses(filter)" (click)="selectedMatchFilter.set(filter)">
								<span>{{ getMatchFilterLabel(filter) }}</span>
								<span class="ml-2 rounded-full bg-white/70 px-2 py-0.5 text-xs dark:bg-slate-950/70">{{ getMatchFilterCount(filter) }}</span>
							</button>
						}
					</div>
				</section>

				@if (selectedMatchFilter() !== "All") {
					@if (filteredMatches().length > 0) {
						<section class="grid gap-4" data-testid="matches-list">
							@for (match of filteredMatches(); track match.id) {
								<ng-container [ngTemplateOutlet]="matchCard" [ngTemplateOutletContext]="{ $implicit: match }" />
							}
						</section>
					} @else {
						<section class="grid gap-5 rounded-2xl border border-dashed border-slate-300 bg-white/80 p-8 text-sm text-slate-600 dark:border-slate-700 dark:bg-slate-950/60 dark:text-slate-300 sm:grid-cols-[1fr_auto] sm:items-center">
							<span>No matches for {{ getMatchFilterLabel(selectedMatchFilter()).toLowerCase() }}. Use All to browse the full group stage schedule.</span>
							<img class="h-28 w-28 object-contain" src="/assets/brand/empty-state-mascot.webp" alt="No matches mascot" />
						</section>
					}
				} @else {
					<section class="grid gap-6" data-testid="matches-list">
						@for (group of groupedFilteredMatches(); track group.dateKey) {
							<div class="grid gap-3">
								<div class="flex items-center justify-between gap-4 rounded-2xl border border-slate-200 bg-white/80 px-4 py-3 shadow-sm dark:border-slate-700 dark:bg-slate-950/70">
									<h2 class="text-sm font-black uppercase tracking-[0.18em] text-slate-700 dark:text-slate-200">{{ group.label }}</h2>
									<span class="rounded-full bg-sky-100 px-3 py-1 text-xs font-bold text-sky-800 dark:bg-sky-950 dark:text-sky-200">{{ group.matches.length }} matches</span>
								</div>
								@for (match of group.matches; track match.id) {
									<ng-container [ngTemplateOutlet]="matchCard" [ngTemplateOutletContext]="{ $implicit: match }" />
								}
							</div>
						}
					</section>
				}

				<ng-template #matchCard let-match>
						<article class="rounded-2xl border border-slate-200 bg-white/90 p-6 shadow-sm dark:border-slate-700 dark:bg-slate-950/80" [attr.data-testid]="'match-card-' + match.id">
							<div class="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
								<div>
									<p class="flex flex-wrap items-center gap-2 text-xs font-bold uppercase tracking-wide text-sky-700 dark:text-sky-300">
										<span>{{ match.stage }}</span>
										@if (getGroupLabel(match)) {
											<span class="rounded-full bg-sky-100 px-2 py-0.5 text-[0.65rem] text-sky-800 dark:bg-sky-950 dark:text-sky-200">{{ getGroupLabel(match) }}</span>
										}
									</p>
									<h2 class="mt-2 text-xl font-bold text-slate-950 dark:text-white">
										{{ match.homeTeamName }} vs {{ match.awayTeamName }}
									</h2>
									<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">{{ match.venue }}</p>
									<div class="mt-4 flex flex-wrap gap-2 text-sm">
										<span class="rounded-full bg-sky-50 px-3 py-1 font-medium text-sky-700">
											Stake: {{ formatCopaCoin(match.stakeAmountCc) }} CC
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
											<span class="rounded-full bg-slate-100 px-3 py-1 font-medium text-slate-700 dark:bg-slate-800 dark:text-slate-200">
												Betting closed
											</span>
										}
										@if (match.officialResult) {
											<span class="rounded-full bg-indigo-50 px-3 py-1 font-medium text-indigo-700" [attr.data-testid]="'match-result-' + match.id">
												Result: {{ getSelectionLabel(match.officialResult, match) }}
											</span>
										}
									<span class="rounded-full px-3 py-1 font-medium" [class]="match.isSettled ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-200' : 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200'" [attr.data-testid]="'match-settlement-status-' + match.id">
											{{ match.isSettled ? 'Settled' : 'Not settled' }}
										</span>
									</div>
								</div>
								<div class="rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-700 dark:bg-slate-900 dark:text-slate-200">
									<p class="font-medium">{{ match.startsAtUtc | date: "medium" : "UTC" }}</p>
									<p class="mt-1 text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">UTC kickoff</p>
								</div>
							</div>

							@if (!match.isSettled) {
							<div class="mt-5 border-t border-slate-100 pt-5 dark:border-slate-800">
								<div class="grid gap-4 lg:grid-cols-[1fr_20rem] lg:items-start">
									<div>
										<p class="text-sm font-medium text-slate-800 dark:text-slate-200">{{ match.currentUserBetSelection ? "Change your winner bet" : "Choose your winner bet" }}</p>
										@if (match.currentUserBetSelection) {
											<p class="mt-1 text-xs text-slate-500 dark:text-slate-400">Current pick: {{ getSelectionLabel(match.currentUserBetSelection, match) }}. You can change it while betting is open.</p>
										}
									<div class="mt-3 flex flex-wrap gap-3">
										@for (selection of betSelections; track selection) {
											<button
												type="button"
												class="rounded-xl border px-4 py-2 text-sm font-medium transition"
												[class]="getBetButtonClasses(match.id, match.isBettingOpen, match.currentUserBetSelection === selection)"
												(click)="placeBet(match.id, selection)"
												[disabled]="isSubmitting(match.id) || !match.isBettingOpen || match.currentUserBetSelection === selection"
												[attr.data-testid]="'place-match-bet-' + match.id + '-' + selection.toLowerCase()"
											>
												{{ getSelectionLabel(selection, match) }}
											</button>
										}
									</div>
									</div>
									@if (getGroupStandings(match).length > 0) {
										<aside class="rounded-2xl border border-slate-200 bg-slate-50/80 p-3 dark:border-slate-700 dark:bg-slate-900/70" [attr.data-testid]="'match-group-standings-' + match.id">
											<p class="text-xs font-black uppercase tracking-[0.18em] text-slate-500 dark:text-slate-400">Group table</p>
											<div class="mt-3 grid gap-2 sm:grid-cols-2 lg:grid-cols-1 xl:grid-cols-2">
												@for (standing of getGroupStandings(match); track standing.groupName + '-' + standing.teamExternalId) {
													<div class="grid grid-cols-[1fr_auto] items-center gap-2 rounded-xl bg-white/80 px-3 py-2 text-xs dark:bg-slate-950/80" [class.ring-2]="isMatchTeamStanding(standing, match)" [class.ring-sky-300]="isMatchTeamStanding(standing, match)" [class.dark:ring-sky-700]="isMatchTeamStanding(standing, match)">
														<div class="min-w-0">
															<p class="truncate font-bold text-slate-900 dark:text-white">{{ getStandingTeamName(standing) }}</p>
															<p class="mt-0.5 text-[0.68rem] text-slate-500 dark:text-slate-400">{{ standing.won }}-{{ standing.drawn }}-{{ standing.lost }} · GD {{ standing.goalDifference }}</p>
														</div>
														<p class="rounded-full bg-amber-100 px-2 py-1 font-black text-amber-800 dark:bg-amber-300 dark:text-slate-950">{{ standing.points }} pts</p>
													</div>
												}
											</div>
										</aside>
									}
								</div>
								</div>
							}

						</article>
				</ng-template>
			}
		</section>
	`,
})
export class MatchesPageComponent {
	private readonly matchesService = inject(MatchesService);
	protected readonly formatCopaCoin = formatCopaCoin;
	readonly betSelections: ReadonlyArray<MatchBetSelection> = ["Home", "Draw", "Away"];
	readonly matchFilters: ReadonlyArray<MatchDayFilter> = ["Today", "Tomorrow", "All"];

	readonly userSummary = signal<CurrentUserSummary | null>(null);
	readonly championMarket = signal<ChampionBetMarket | null>(null);
	readonly footballData = signal<FootballDataSnapshot | null>(null);
	readonly matches = signal<ReadonlyArray<MatchListItem>>([]);
	readonly isLoading = signal(true);
	readonly errorMessage = signal("");
	readonly successMessage = signal("");
	readonly selectedChampionTeamName = signal("");
	readonly selectedMatchFilter = signal<MatchDayFilter>("Today");
	readonly isSubmittingChampionBet = signal(false);
	readonly submittingMatchId = signal<number | null>(null);
	readonly showDashboard = computed(() => !this.isLoading() && !!this.userSummary() && !!this.championMarket());
	readonly filteredMatches = computed(() => this.filterMatches(this.selectedMatchFilter()));
	readonly groupedFilteredMatches = computed(() => this.groupMatchesByDate(this.filteredMatches()));
	readonly placedMatchBets = computed(() => this.matches().filter((match) => !!match.currentUserBetSelection));
	readonly pendingStakeAmountCc = computed(() => {
		const pendingMatchStakeAmountCc = this.placedMatchBets()
			.filter((match) => !match.isSettled)
			.reduce((total, match) => total + match.stakeAmountCc, 0);
		const championMarket = this.championMarket();
		const pendingChampionStakeAmountCc = championMarket?.currentUserChampionTeamName && !championMarket.isSettled
			? championMarket.stakeAmountCc
			: 0;

		return pendingMatchStakeAmountCc + pendingChampionStakeAmountCc;
	});
	readonly availableBalanceCc = computed(() => this.userSummary()?.currentBalanceCc ?? 0);
	readonly realizedBalanceCc = computed(() => this.availableBalanceCc() + this.pendingStakeAmountCc());

	constructor() {
		this.loadPageData();
	}

	placeBet(matchId: number, selection: MatchBetSelection): void {
		this.errorMessage.set("");
		this.successMessage.set("");
		this.submittingMatchId.set(matchId);

		this.matchesService.placeMatchBet({ matchId, selection }).subscribe({
			next: (result) => {
				const placedMatch = this.matches().find((match) => match.id === matchId);
				this.matches.set(this.matches().map((match) =>
					match.id === result.matchId
						? { ...match, currentUserBetSelection: result.selection }
						: match,
				));
				this.successMessage.set(`Bet placed for ${this.getSelectionLabel(selection, placedMatch)}. Remaining balance: ${formatCopaCoin(result.remainingBalanceCc)} CC.`);
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

				this.successMessage.set(`Champion bet placed for ${result.teamName}. Remaining balance: ${formatCopaCoin(result.remainingBalanceCc)} CC.`);
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

	getBetButtonClasses(matchId: number, isBettingOpen: boolean, isCurrentSelection = false): string {
		if (this.isSubmitting(matchId)) {
			return "border-sky-200 bg-sky-50 text-sky-700 dark:border-sky-900 dark:bg-sky-950 dark:text-sky-200 cursor-not-allowed opacity-60";
		}

		if (isCurrentSelection) {
			return "border-emerald-300 bg-emerald-50 text-emerald-800 dark:border-emerald-800 dark:bg-emerald-950 dark:text-emerald-200 cursor-not-allowed";
		}

		if (!isBettingOpen) {
			return "border-slate-200 bg-slate-50 text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400 cursor-not-allowed opacity-60";
		}

		return "border-slate-300 bg-white text-slate-800 hover:border-sky-400 hover:text-sky-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:hover:border-sky-400 dark:hover:text-sky-200";
	}

	getMatchFilterLabel(filter: MatchDayFilter): string {
		return filter === "Today" ? "Next matchday" : filter === "Tomorrow" ? "Following day" : "All";
	}

	getMatchFilterCount(filter: MatchDayFilter): number {
		return this.filterMatches(filter).length;
	}

	getMatchFilterClasses(filter: MatchDayFilter): string {
		return this.selectedMatchFilter() === filter
			? "bg-slate-950 text-white shadow-lg shadow-slate-900/15 dark:bg-amber-300 dark:text-slate-950"
			: "bg-white/70 text-slate-700 hover:bg-white dark:bg-slate-900/70 dark:text-slate-200 dark:hover:bg-slate-900";
	}

	getGroupLabel(match: MatchListItem): string {
		const groups = this.getMatchGroups(match);
		if (groups.length === 0) {
			return "";
		}

		return groups.length === 1 ? groups[0] : groups.join(" / ");
	}

	getGroupStandings(match: MatchListItem): ReadonlyArray<FootballGroupStanding> {
		const groups = this.getMatchGroups(match);
		if (groups.length === 0) {
			return [];
		}

		return (this.footballData()?.groupStandings ?? [])
			.filter((standing) => groups.includes(standing.groupName))
			.sort((left, right) =>
				groups.indexOf(left.groupName) - groups.indexOf(right.groupName) ||
				right.points - left.points ||
				right.goalDifference - left.goalDifference ||
				right.goalsFor - left.goalsFor,
			);
	}

	getStandingTeamName(standing: FootballGroupStanding): string {
		return this.footballData()?.teams.find((team) => team.externalId === standing.teamExternalId)?.nameEn ?? standing.teamExternalId;
	}

	isMatchTeamStanding(standing: FootballGroupStanding, match: MatchListItem): boolean {
		const homeTeam = this.getFootballTeam(match.homeTeamName);
		const awayTeam = this.getFootballTeam(match.awayTeamName);
		return standing.teamExternalId === homeTeam?.externalId || standing.teamExternalId === awayTeam?.externalId;
	}

	private loadPageData(): void {
		forkJoin({
			userSummary: this.matchesService.getCurrentUserSummary(),
			championMarket: this.matchesService.getChampionBetMarket(),
			matches: this.matchesService.listMatches(),
			footballData: this.matchesService.getFootballDataSnapshot(),
		}).subscribe({
			next: ({ userSummary, championMarket, matches, footballData }) => {
				this.userSummary.set(userSummary);
				this.championMarket.set(championMarket);
				this.matches.set(matches);
				this.footballData.set(footballData);
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
			error: () => {
				this.errorMessage.set("Your bet was saved, but the wallet could not refresh. Reload the page to confirm the latest balance.");
			},
		});
	}

	private getMatchGroups(match: MatchListItem): ReadonlyArray<string> {
		return [match.groupName]
			.filter((groupName): groupName is string => !!groupName)
			.filter((groupName, index, groups) => groups.indexOf(groupName) === index);
	}

	private getFootballTeam(teamName: string): FootballTeam | undefined {
		const normalizedTeamName = this.normalizeTeamName(teamName);
		return this.footballData()?.teams.find((team) => this.normalizeTeamName(team.nameEn) === normalizedTeamName);
	}

	private normalizeTeamName(teamName: string): string {
		return teamName.trim().toLocaleLowerCase();
	}

	private filterMatches(filter: MatchDayFilter): ReadonlyArray<MatchListItem> {
		if (filter === "All") {
			return this.matches();
		}

		const targetDateKey = this.getRelativeMatchDateKey(filter === "Today" ? 0 : 1);
		if (!targetDateKey) {
			return [];
		}

		return this.matches().filter((match) => this.getMatchDateKey(match) === targetDateKey);
	}

	private groupMatchesByDate(matches: ReadonlyArray<MatchListItem>): ReadonlyArray<MatchDateGroup> {
		const groups = new Map<string, MatchListItem[]>();
		for (const match of matches) {
			const dateKey = this.getMatchDateKey(match);
			groups.set(dateKey, [...(groups.get(dateKey) ?? []), match]);
		}

		return [...groups.entries()].map(([dateKey, groupMatches]) => ({
			dateKey,
			label: this.getDateGroupLabel(dateKey),
			matches: groupMatches,
		}));
	}

	private getRelativeMatchDateKey(offsetDays: number): string {
		const dateKeys = this.matches()
			.map((match) => this.getMatchDateKey(match))
			.filter((dateKey, index, dateKeys) => dateKeys.indexOf(dateKey) === index)
			.sort();

		return dateKeys[offsetDays] ?? "";
	}

	private getMatchDateKey(match: MatchListItem): string {
		return this.formatDateKey(new Date(match.startsAtUtc));
	}

	private formatDateKey(date: Date): string {
		return date.toISOString().slice(0, 10);
	}

	private getDateGroupLabel(dateKey: string): string {
		return new Intl.DateTimeFormat("en", {
			dateStyle: "full",
			timeZone: "UTC",
		}).format(new Date(`${dateKey}T00:00:00Z`));
	}
}
