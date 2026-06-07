import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { catchError, debounceTime, distinctUntilChanged, forkJoin, of, Subject, switchMap } from "rxjs";
import { formatCopaCoin } from "../../shared/copa-coin-format";
import { MatchesService } from "../matches/matches.service";
import type { ChampionBetMarket, CurrentUserSummary, MatchBetSelection, MatchListItem, PlayerSearchResult, SpecialBetMarket, SpecialPlayerBet, SpecialPlayerBetCategory } from "../matches/matches.models";

interface PlayerBetDefinition {
	category: SpecialPlayerBetCategory;
	label: string;
	description: string;
	placeholder: string;
}

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
							This page is your personal betting ledger: active picks, tournament specials, settled results, and wallet context.
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

			@if (!isLoading() && userSummary() && championMarket() && specialMarket()) {
				<section class="grid gap-4 md:grid-cols-3">
					<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
						<p class="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">Total settled</p>
						<p class="mt-2 text-3xl font-black text-slate-950 dark:text-white">{{ formatCopaCoin(realizedBalanceCc()) }} CC</p>
						<div class="mt-2 flex flex-wrap gap-2 text-xs font-bold">
							@if (pendingStakeAmountCc() > 0) {
								<p class="rounded-full bg-amber-50 px-3 py-1 text-amber-700 dark:bg-amber-950 dark:text-amber-200">{{ formatCopaCoin(pendingStakeAmountCc()) }} CC pending</p>
							}
							<p class="rounded-full bg-slate-100 px-3 py-1 text-slate-600 dark:bg-slate-800 dark:text-slate-300">{{ formatCopaCoin(availableBalanceCc()) }} CC available</p>
						</div>
						<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">{{ userSummary()!.displayName }}</p>
					</article>
					<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
						<p class="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">Match picks</p>
						<p class="mt-2 text-3xl font-black text-slate-950 dark:text-white">{{ placedMatchBets().length }}</p>
						<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">{{ settledPickCount() }} settled</p>
					</article>
					<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
						<p class="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">Tournament picks</p>
						<p class="mt-2 text-3xl font-black text-slate-950 dark:text-white">{{ tournamentPickCount() }}/3</p>
						<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">{{ formatCopaCoin(tournamentStakeAmountCc()) }} CC pending</p>
					</article>
				</section>

				<section class="grid gap-4" data-testid="tournament-special-bets">
					<div class="grid gap-4 lg:grid-cols-3">
						<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80" data-testid="champion-market-card">
							<p class="text-xs font-bold uppercase tracking-[0.2em] text-sky-700 dark:text-sky-300">Champion bet</p>
							<div class="mt-2 flex items-start justify-between gap-3">
								<div class="min-w-0">
									<p class="text-2xl font-black text-slate-950 dark:text-white">{{ championMarket()!.currentUserChampionTeamName ?? "Pending" }}</p>
									<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">Stake: {{ formatCopaCoin(championMarket()!.stakeAmountCc) }} CC</p>
									@if (championMarket()!.currentUserChampionTeamName) {
										<p class="mt-4 inline-flex rounded-full bg-emerald-50 px-3 py-1 text-sm font-medium text-emerald-700" data-testid="champion-current-pick">Selected</p>
									}
								</div>
								@if (championMarket()!.currentUserChampionTeamFlagUrl) {
									<img class="h-16 w-16 shrink-0 rounded-full object-cover" [src]="championMarket()!.currentUserChampionTeamFlagUrl" [alt]="championMarket()!.currentUserChampionTeamName!" />
								}
							</div>
							@if (!championMarket()!.currentUserChampionTeamName) {
								<div class="mt-4 grid gap-3">
									<select #championTeamSelect class="min-w-0 rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-white" (change)="selectedChampionTeamName.set(championTeamSelect.value)" data-testid="champion-team-select">
										<option value="">Select a team</option>
										@for (teamName of championMarket()!.teamOptions; track teamName) {
											<option [value]="teamName">{{ teamName }}</option>
										}
									</select>
									<button type="button" class="rounded-xl border border-sky-600 bg-sky-600 px-4 py-3 text-sm font-medium text-white transition hover:bg-sky-700 disabled:cursor-not-allowed disabled:opacity-60" (click)="placeChampionBet()" [disabled]="!selectedChampionTeamName() || !championMarket()!.isBettingOpen || isSubmittingChampionBet()" data-testid="place-champion-bet-button">
										Place champion bet
									</button>
								</div>
							}
						</article>

						@for (definition of playerBetDefinitions; track definition.category) {
							<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80" [attr.data-testid]="'special-player-bet-' + definition.category">
								<p class="text-xs font-bold uppercase tracking-[0.2em] text-sky-700 dark:text-sky-300">{{ definition.label }}</p>
								@if (getSpecialPlayerBet(definition.category); as existingBet) {
									<div class="mt-2 flex items-start justify-between gap-3">
										<div class="min-w-0">
											<p class="text-2xl font-black text-slate-950 dark:text-white">{{ existingBet.playerName }}</p>
											<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">Stake: {{ formatCopaCoin(existingBet.stakeAmountCc) }} CC</p>
											<p class="mt-4 inline-flex rounded-full bg-emerald-50 px-3 py-1 text-sm font-medium text-emerald-700">Selected</p>
										</div>
										@if (existingBet.playerPhotoUrl) {
											<img class="h-16 w-16 shrink-0 rounded-full object-cover" [src]="existingBet.playerPhotoUrl" [alt]="existingBet.playerName" />
										}
									</div>
								} @else {
									<p class="mt-2 text-sm text-slate-600 dark:text-slate-300">{{ definition.description }}</p>
									<div class="mt-4 grid gap-3">
										<input class="min-w-0 rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-white" [placeholder]="definition.placeholder" [value]="getSelectedPlayerName(definition.category)" (input)="onPlayerInput(definition.category, $any($event.target).value)" [attr.data-testid]="'special-player-search-' + definition.category" />
										@if (getPlayerOptions(definition.category).length > 0) {
											<div class="grid gap-2 rounded-2xl border border-slate-200 bg-slate-50 p-2 dark:border-slate-700 dark:bg-slate-900/70" [attr.data-testid]="'special-player-options-' + definition.category">
												@for (player of getPlayerOptions(definition.category); track player.externalId) {
													<button type="button" class="flex items-center gap-3 rounded-xl border px-3 py-2 text-left transition hover:border-sky-300 hover:bg-white dark:hover:border-sky-700 dark:hover:bg-slate-950" [class]="getPlayerOptionClasses(definition.category, player)" (click)="selectPlayer(definition.category, player)">
														@if (player.thumbnailUrl) {
															<img class="h-10 w-10 rounded-full object-cover" [src]="player.thumbnailUrl" [alt]="player.name" />
														}
														<span class="min-w-0">
															<span class="block truncate text-sm font-bold text-slate-950 dark:text-white">{{ player.name }}</span>
															<span class="block truncate text-xs text-slate-600 dark:text-slate-300">{{ getPlayerOptionDetail(player) }}</span>
														</span>
													</button>
												}
											</div>
										}
										<button type="button" class="rounded-xl border border-sky-600 bg-sky-600 px-4 py-3 text-sm font-medium text-white transition hover:bg-sky-700 disabled:cursor-not-allowed disabled:opacity-60" (click)="placeSpecialPlayerBet(definition.category)" [disabled]="!getSelectedPlayer(definition.category) || !specialMarket()!.isBettingOpen || submittingSpecialCategory() === definition.category" [attr.data-testid]="'place-special-player-bet-' + definition.category">
											Place {{ definition.label.toLowerCase() }} bet
										</button>
									</div>
								}
							</article>
						}
					</div>
					@if (!championMarket()!.isBettingOpen || !specialMarket()!.isBettingOpen) {
						<p class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300">Tournament special betting is closed.</p>
					} @else if (championMarket()!.bettingClosesAtUtc) {
						<p class="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-200">These picks close at the start of Round of 32: {{ championMarket()!.bettingClosesAtUtc | date: "medium" : "UTC" }} UTC.</p>
					}
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
										<span class="rounded-full bg-amber-50 px-3 py-1 text-amber-700 dark:bg-amber-950 dark:text-amber-200">Stake: {{ formatCopaCoin(match.stakeAmountCc) }} CC</span>
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
	protected readonly formatCopaCoin = formatCopaCoin;
	readonly betSelections: ReadonlyArray<MatchBetSelection> = ["Home", "Draw", "Away"];
	readonly playerBetDefinitions: ReadonlyArray<PlayerBetDefinition> = [
		{ category: "BestPlayer", label: "Best player", description: "Pick the tournament's best player.", placeholder: "Type at least 3 characters" },
		{ category: "TopScorer", label: "Top scorer", description: "Pick the tournament's leading goal scorer.", placeholder: "Type at least 3 characters" },
	];

	readonly userSummary = signal<CurrentUserSummary | null>(null);
	readonly championMarket = signal<ChampionBetMarket | null>(null);
	readonly specialMarket = signal<SpecialBetMarket | null>(null);
	readonly matches = signal<ReadonlyArray<MatchListItem>>([]);
	readonly isLoading = signal(true);
	readonly errorMessage = signal("");
	readonly successMessage = signal("");
	readonly selectedChampionTeamName = signal("");
	readonly selectedBestPlayerName = signal("");
	readonly selectedTopScorerName = signal("");
	readonly selectedBestPlayerExternalId = signal<string | null>(null);
	readonly selectedTopScorerExternalId = signal<string | null>(null);
	readonly bestPlayerOptions = signal<ReadonlyArray<PlayerSearchResult>>([]);
	readonly topScorerOptions = signal<ReadonlyArray<PlayerSearchResult>>([]);
	readonly isSubmittingChampionBet = signal(false);
	readonly submittingSpecialCategory = signal<SpecialPlayerBetCategory | null>(null);
	readonly submittingMatchId = signal<number | null>(null);
	private readonly bestPlayerSearch = new Subject<string>();
	private readonly topScorerSearch = new Subject<string>();

	readonly placedMatchBets = computed(() => this.matches().filter((match) => !!match.currentUserBetSelection));
	readonly settledPickCount = computed(() => this.placedMatchBets().filter((match) => match.isSettled).length);
	readonly tournamentPickCount = computed(() => (this.championMarket()?.currentUserChampionTeamName ? 1 : 0) + (this.specialMarket()?.playerBets.length ?? 0));
	readonly tournamentStakeAmountCc = computed(() => {
		const championStake = this.championMarket()?.currentUserChampionTeamName && !this.championMarket()?.isSettled
			? this.championMarket()!.stakeAmountCc
			: 0;
		const playerStake = this.specialMarket()?.playerBets.reduce((total, bet) => total + bet.stakeAmountCc, 0) ?? 0;
		return championStake + playerStake;
	});
	readonly pendingStakeAmountCc = computed(() => {
		const pendingMatchStakeAmountCc = this.placedMatchBets()
			.filter((match) => !match.isSettled)
			.reduce((total, match) => total + match.stakeAmountCc, 0);

		return pendingMatchStakeAmountCc + this.tournamentStakeAmountCc();
	});
	readonly availableBalanceCc = computed(() => this.userSummary()?.currentBalanceCc ?? 0);
	readonly realizedBalanceCc = computed(() => this.availableBalanceCc() + this.pendingStakeAmountCc());

	constructor() {
		this.connectPlayerSearch(this.bestPlayerSearch, this.bestPlayerOptions);
		this.connectPlayerSearch(this.topScorerSearch, this.topScorerOptions);
		this.loadPageData();
	}

	getSelectionLabel(selection: MatchBetSelection, match: MatchListItem): string {
		return selection === "Home" ? match.homeTeamName : selection === "Away" ? match.awayTeamName : "Draw";
	}

	onPlayerInput(category: SpecialPlayerBetCategory, value: string): void {
		if (category === "BestPlayer") {
			this.selectedBestPlayerName.set(value);
			this.selectedBestPlayerExternalId.set(null);
			this.bestPlayerSearch.next(value);
			return;
		}

		this.selectedTopScorerName.set(value);
		this.selectedTopScorerExternalId.set(null);
		this.topScorerSearch.next(value);
	}

	selectPlayer(category: SpecialPlayerBetCategory, player: PlayerSearchResult): void {
		if (category === "BestPlayer") {
			this.selectedBestPlayerName.set(player.name);
			this.selectedBestPlayerExternalId.set(player.externalId);
			return;
		}

		this.selectedTopScorerName.set(player.name);
		this.selectedTopScorerExternalId.set(player.externalId);
	}

	getSelectedPlayerName(category: SpecialPlayerBetCategory): string {
		return category === "BestPlayer" ? this.selectedBestPlayerName() : this.selectedTopScorerName();
	}

	getPlayerOptions(category: SpecialPlayerBetCategory): ReadonlyArray<PlayerSearchResult> {
		return category === "BestPlayer" ? this.bestPlayerOptions() : this.topScorerOptions();
	}

	getSelectedPlayer(category: SpecialPlayerBetCategory): PlayerSearchResult | undefined {
		const externalId = category === "BestPlayer" ? this.selectedBestPlayerExternalId() : this.selectedTopScorerExternalId();
		return this.getPlayerOptions(category).find((player) => player.externalId === externalId);
	}

	getPlayerOptionClasses(category: SpecialPlayerBetCategory, player: PlayerSearchResult): string {
		return this.getSelectedPlayer(category)?.externalId === player.externalId
			? "border-sky-400 bg-white ring-2 ring-sky-200 dark:border-sky-600 dark:bg-slate-950 dark:ring-sky-900"
			: "border-transparent bg-transparent";
	}

	getPlayerOptionDetail(player: PlayerSearchResult): string {
		return [player.teamName, player.nationality, player.position].filter(Boolean).join(" · ") || "Soccer player";
	}

	getSpecialPlayerBet(category: SpecialPlayerBetCategory): SpecialPlayerBet | undefined {
		return this.specialMarket()?.playerBets.find((bet) => bet.category === category);
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
				this.championMarket.set({ ...this.championMarket()!, currentUserChampionTeamName: result.teamName });
				this.successMessage.set(`Champion bet placed for ${result.teamName}. Remaining balance: ${formatCopaCoin(result.remainingBalanceCc)} CC.`);
				this.selectedChampionTeamName.set("");
				this.refreshUserSummary();
				this.isSubmittingChampionBet.set(false);
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? "Unable to place the champion bet right now.");
				this.isSubmittingChampionBet.set(false);
			},
		});
	}

	placeSpecialPlayerBet(category: SpecialPlayerBetCategory): void {
		const playerName = this.getSelectedPlayerName(category).trim();
		if (playerName.length < 3) {
			return;
		}

		const selectedPlayer = this.getSelectedPlayer(category);
		if (!selectedPlayer) {
			return;
		}

		this.errorMessage.set("");
		this.successMessage.set("");
		this.submittingSpecialCategory.set(category);

		this.matchesService.placeSpecialPlayerBet({ category, playerName, externalPlayerId: selectedPlayer.externalId }).subscribe({
			next: (result) => {
				this.specialMarket.set({
					...this.specialMarket()!,
					playerBets: [
						...this.specialMarket()!.playerBets,
						{ category: result.category, playerName: result.playerName, externalPlayerId: result.externalPlayerId, playerPhotoUrl: selectedPlayer.thumbnailUrl, stakeAmountCc: result.stakeAmountCc, placedAtUtc: result.placedAtUtc },
					],
				});
				this.successMessage.set(`${this.getPlayerBetLabel(category)} bet placed for ${result.playerName}. Remaining balance: ${formatCopaCoin(result.remainingBalanceCc)} CC.`);
				this.clearSelectedPlayer(category);
				this.refreshUserSummary();
				this.submittingSpecialCategory.set(null);
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? "Unable to place the player bet right now.");
				this.submittingSpecialCategory.set(null);
			},
		});
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

	private loadPageData(): void {
		forkJoin({
			userSummary: this.matchesService.getCurrentUserSummary(),
			championMarket: this.matchesService.getChampionBetMarket(),
			specialMarket: this.matchesService.getSpecialBetMarket(),
			matches: this.matchesService.listMatches(),
		}).subscribe({
			next: ({ userSummary, championMarket, specialMarket, matches }) => {
				this.userSummary.set(userSummary);
				this.championMarket.set(championMarket);
				this.specialMarket.set(specialMarket);
				this.matches.set(matches);
				this.isLoading.set(false);
			},
			error: () => {
				this.errorMessage.set("Unable to load your bets right now.");
				this.isLoading.set(false);
			},
		});
	}

	private refreshUserSummary(): void {
		this.matchesService.getCurrentUserSummary().subscribe({
			next: (userSummary) => this.userSummary.set(userSummary),
			error: () => this.errorMessage.set("Your bet was saved, but the wallet could not refresh. Reload the page to confirm the latest balance."),
		});
	}

	private connectPlayerSearch(search: Subject<string>, target: { set: (value: ReadonlyArray<PlayerSearchResult>) => void }): void {
		search.pipe(
			debounceTime(250),
			distinctUntilChanged(),
			switchMap((query) => query.trim().length < 3
				? of([])
				: this.matchesService.searchPlayers(query).pipe(catchError(() => of([])))),
		).subscribe((players) => target.set(players));
	}

	private clearSelectedPlayer(category: SpecialPlayerBetCategory): void {
		if (category === "BestPlayer") {
			this.selectedBestPlayerName.set("");
			this.selectedBestPlayerExternalId.set(null);
			this.bestPlayerOptions.set([]);
			return;
		}

		this.selectedTopScorerName.set("");
		this.selectedTopScorerExternalId.set(null);
		this.topScorerOptions.set([]);
	}

	private getPlayerBetLabel(category: SpecialPlayerBetCategory): string {
		return this.playerBetDefinitions.find((definition) => definition.category === category)?.label ?? "Player";
	}
}
