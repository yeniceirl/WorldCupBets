import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { forkJoin } from "rxjs";
import { AuthStateService } from "../../core/auth/auth-state.service";
import { formatCopaCoin } from "../../shared/copa-coin-format";
import { AdminService } from "./admin.service";
import { MatchesService } from "../matches/matches.service";
import type { ChampionBetMarket, MatchBetSelection, MatchListItem } from "../matches/matches.models";
import type { CreateUserInvitationRequest } from "./admin.models";

@Component({
	selector: "app-admin-page",
	imports: [DatePipe],
	template: `
		<section class="space-y-6">
			<header class="overflow-hidden rounded-[2rem] border border-slate-900 bg-slate-950 p-6 text-white shadow-xl shadow-slate-900/20 dark:border-amber-300 dark:bg-amber-300 dark:text-slate-950">
				<div class="grid gap-5 sm:grid-cols-[1fr_auto] sm:items-center">
					<div>
						<p class="text-sm font-bold uppercase tracking-[0.24em] opacity-75">Admin control room</p>
						<h1 class="mt-2 text-4xl font-black tracking-tight">Operate the tournament</h1>
						<p class="mt-3 max-w-2xl text-sm leading-6 opacity-80">Record official match results, trigger settlement, and close the champion market when the tournament is decided.</p>
					</div>
					<img class="mx-auto h-32 w-32 rounded-3xl object-cover shadow-2xl shadow-black/30 sm:mx-0" src="/assets/brand/admin-control-room.webp" alt="CopaCoin admin control room" />
				</div>
			</header>

			@if (!isAdmin()) {
				<section class="rounded-2xl border border-amber-200 bg-amber-50 p-5 text-sm text-amber-800 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-200">
					You are signed in, but your current dev persona is not Admin. Use Dev Admin from the login page to execute admin actions.
				</section>
			}

			@if (successMessage()) {
				<section class="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-200" data-testid="success-message">{{ successMessage() }}</section>
			}

			@if (errorMessage()) {
				<section class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-200" data-testid="error-message">{{ errorMessage() }}</section>
			}

			<section class="rounded-2xl border border-sky-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
				<div class="grid gap-4 lg:grid-cols-[1fr_auto] lg:items-center">
					<div>
						<p class="text-xs font-bold uppercase tracking-[0.2em] text-sky-700 dark:text-sky-300">External football data</p>
						<h2 class="mt-2 text-2xl font-black text-slate-950 dark:text-white">Sync and import group fixtures</h2>
						<p class="mt-2 max-w-3xl text-sm leading-6 text-slate-600 dark:text-slate-300">First sync the provider snapshot, then import group stage fixtures into our CopaCoin schedule. Import is idempotent and keeps our database as the betting source of truth.</p>
					</div>
					<div class="flex flex-col gap-2 sm:flex-row lg:flex-col">
						<button type="button" class="rounded-xl border border-sky-600 bg-sky-600 px-4 py-3 text-sm font-bold text-white transition hover:bg-sky-700 disabled:cursor-not-allowed disabled:opacity-60" [disabled]="!isAdmin() || isSyncingFootballData()" (click)="syncFootballData()" data-testid="admin-sync-provider">{{ isSyncingFootballData() ? "Syncing..." : "Sync provider" }}</button>
						<button type="button" class="rounded-xl border border-emerald-600 bg-emerald-600 px-4 py-3 text-sm font-bold text-white transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60" [disabled]="!isAdmin() || isImportingFixtures()" (click)="importGroupStageFixtures()" data-testid="admin-import-fixtures">{{ isImportingFixtures() ? "Importing..." : "Import group fixtures" }}</button>
					</div>
				</div>
			</section>

			<section class="rounded-2xl border border-violet-200 bg-white/90 p-5 shadow-sm dark:border-violet-900/70 dark:bg-slate-950/80">
				<div class="grid gap-4 lg:grid-cols-[1fr_auto] lg:items-center">
					<div>
						<p class="text-xs font-bold uppercase tracking-[0.2em] text-violet-700 dark:text-violet-300">Player search index</p>
						<h2 class="mt-2 text-2xl font-black text-slate-950 dark:text-white">Sync player squads</h2>
						<p class="mt-2 max-w-3xl text-sm leading-6 text-slate-600 dark:text-slate-300">Fetches squads for the configured national teams from API-Sports and replaces the persisted player search index. This calls the external API directly, so only run it when needed.</p>
						@if (lastPlayerSyncAtUtc()) {
							<p class="mt-3 text-xs font-semibold uppercase tracking-[0.18em] text-violet-700 dark:text-violet-300" data-testid="admin-players-last-synced">Last synced {{ lastPlayerSyncAtUtc() | date: "medium" : "UTC" }} UTC</p>
						}
					</div>
					<button type="button" class="rounded-xl border border-violet-600 bg-violet-600 px-4 py-3 text-sm font-bold text-white transition hover:bg-violet-700 disabled:cursor-not-allowed disabled:opacity-60" [disabled]="!isAdmin() || isSyncingPlayers()" (click)="syncPlayerSquads()" data-testid="admin-sync-players">{{ isSyncingPlayers() ? "Syncing..." : "Sync players" }}</button>
				</div>
			</section>

			<section class="rounded-2xl border border-amber-200 bg-white/90 p-5 shadow-sm dark:border-amber-900/70 dark:bg-slate-950/80">
				<div class="grid gap-5 lg:grid-cols-[1fr_1.1fr] lg:items-end">
					<div>
						<p class="text-xs font-bold uppercase tracking-[0.2em] text-amber-700 dark:text-amber-300">Private league access</p>
						<h2 class="mt-2 text-2xl font-black text-slate-950 dark:text-white">Invite a friend</h2>
						<p class="mt-2 max-w-2xl text-sm leading-6 text-slate-600 dark:text-slate-300">Add the Google account email before they sign in. The backend will reject any first-time Google user who is not on this list.</p>
					</div>
					<form class="grid gap-3 sm:grid-cols-[1fr_10rem_auto]" (submit)="$event.preventDefault(); createInvitation(inviteEmail.value, inviteRole.value); inviteEmail.value = ''">
						<input #inviteEmail type="email" autocomplete="email" required placeholder="friend@gmail.com" class="rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-amber-500 focus:ring-2 focus:ring-amber-200 dark:border-slate-700 dark:bg-slate-950 dark:text-white dark:focus:ring-amber-900" data-testid="admin-invite-email" />
						<select #inviteRole class="rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-white" data-testid="admin-invite-role">
							<option value="Bettor">Bettor</option>
							<option value="Admin">Admin</option>
						</select>
						<button type="submit" class="rounded-xl bg-amber-500 px-5 py-3 text-sm font-black text-slate-950 transition hover:bg-amber-400 disabled:cursor-not-allowed disabled:opacity-60" [disabled]="!isAdmin() || isCreatingInvitation()" data-testid="admin-create-invitation">{{ isCreatingInvitation() ? "Inviting..." : "Invite" }}</button>
					</form>
				</div>
			</section>

			<section class="grid gap-4 lg:grid-cols-[1.3fr_0.9fr]">
				<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
					<div class="flex items-start justify-between gap-4">
						<div>
							<p class="text-xs font-bold uppercase tracking-[0.2em] text-sky-700 dark:text-sky-300">Match settlement queue</p>
							<h2 class="mt-2 text-2xl font-black text-slate-950 dark:text-white">Closed and waiting</h2>
						</div>
						<button type="button" class="rounded-full border border-slate-200 px-3 py-2 text-sm font-medium text-slate-700 transition hover:border-sky-300 dark:border-slate-700 dark:text-slate-200" (click)="loadData()">Refresh</button>
					</div>

					@if (isLoading()) {
						<p class="mt-5 text-sm text-slate-600 dark:text-slate-300">Loading matches...</p>
					} @else if (settlementQueue().length === 0) {
						<p class="mt-5 rounded-xl border border-dashed border-slate-300 p-4 text-sm text-slate-600 dark:border-slate-700 dark:text-slate-300">No closed unsettled matches right now.</p>
					} @else {
						<div class="mt-5 grid gap-3">
							@for (match of settlementQueue(); track match.id) {
								<div class="rounded-xl border border-slate-200 bg-slate-50 p-4 dark:border-slate-700 dark:bg-slate-900">
									<div class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
										<div>
											<p class="text-xs font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">{{ match.stage }}</p>
											<h3 class="mt-1 font-bold text-slate-950 dark:text-white">{{ match.homeTeamName }} vs {{ match.awayTeamName }}</h3>
									<p class="text-xs text-slate-500 dark:text-slate-400">Closed {{ match.bettingClosesAtUtc | date: "short" }}</p>
										</div>
										<div class="flex flex-col gap-2 sm:flex-row">
											<select #resultSelect class="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-white" (change)="setSelectedResult(match.id, resultSelect.value)" [attr.data-testid]="'admin-result-select-' + match.id">
												<option value="">Result</option>
												<option value="Home">{{ match.homeTeamName }}</option>
												<option value="Draw">Draw</option>
												<option value="Away">{{ match.awayTeamName }}</option>
											</select>
											<button type="button" class="rounded-xl bg-slate-950 px-4 py-2 text-sm font-bold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-amber-300 dark:text-slate-950" [disabled]="!isAdmin() || !selectedResultByMatchId()[match.id] || submittingMatchId() === match.id" (click)="recordResult(match)" [attr.data-testid]="'admin-record-result-' + match.id">Record</button>
										</div>
									</div>
								</div>
							}
						</div>
					}
				</article>

				<article class="rounded-2xl border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
					<p class="text-xs font-bold uppercase tracking-[0.2em] text-sky-700 dark:text-sky-300">Champion settlement</p>
					<h2 class="mt-2 text-2xl font-black text-slate-950 dark:text-white">Final payout</h2>
					<p class="mt-3 text-sm leading-6 text-slate-600 dark:text-slate-300">Use this after the champion is official. It distributes champion stakes and jackpot once.</p>
					<select #championSelect class="mt-5 w-full rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-white" (change)="selectedChampionTeamName.set(championSelect.value)" data-testid="admin-champion-select">
						<option value="">Select champion</option>
						@for (teamName of championMarket()?.teamOptions ?? []; track teamName) {
							<option [value]="teamName">{{ teamName }}</option>
						}
					</select>
					<button type="button" class="mt-3 w-full rounded-xl bg-emerald-600 px-4 py-3 text-sm font-bold text-white transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60" [disabled]="!isAdmin() || !selectedChampionTeamName() || isSettlingChampion()" (click)="settleChampion()" data-testid="admin-settle-champion">Settle champion</button>
				</article>
			</section>
		</section>
	`,
})
export class AdminPageComponent {
	private readonly authState = inject(AuthStateService);
	private readonly adminService = inject(AdminService);
	private readonly matchesService = inject(MatchesService);
	protected readonly formatCopaCoin = formatCopaCoin;

	readonly matches = signal<ReadonlyArray<MatchListItem>>([]);
	readonly championMarket = signal<ChampionBetMarket | null>(null);
	readonly isLoading = signal(true);
	readonly errorMessage = signal("");
	readonly successMessage = signal("");
	readonly selectedResultByMatchId = signal<Record<number, MatchBetSelection | "">>({});
	readonly selectedChampionTeamName = signal("");
	readonly submittingMatchId = signal<number | null>(null);
	readonly isSettlingChampion = signal(false);
	readonly isSyncingFootballData = signal(false);
	readonly isSyncingPlayers = signal(false);
	readonly lastPlayerSyncAtUtc = signal<string | null>(this.adminService.getLastPlayerSyncAtUtc());
	readonly isImportingFixtures = signal(false);
	readonly isCreatingInvitation = signal(false);
	readonly settlementQueue = computed(() => this.matches().filter((match) => !match.isBettingOpen && !match.isSettled));

	constructor() {
		this.loadData();
	}

	isAdmin(): boolean {
		return this.authState.user()?.roles.includes("Admin") ?? false;
	}

	loadData(): void {
		this.errorMessage.set("");
		this.isLoading.set(true);
		forkJoin({
			matches: this.matchesService.listMatches(),
			championMarket: this.matchesService.getChampionBetMarket(),
		}).subscribe({
			next: ({ matches, championMarket }) => {
				this.matches.set(matches);
				this.championMarket.set(championMarket);
				this.isLoading.set(false);
			},
			error: () => {
				this.errorMessage.set("Unable to load admin data.");
				this.isLoading.set(false);
			},
		});
	}

	setSelectedResult(matchId: number, selection: string): void {
		this.selectedResultByMatchId.set({
			...this.selectedResultByMatchId(),
			[matchId]: this.isSelection(selection) ? selection : "",
		});
	}

	recordResult(match: MatchListItem): void {
		const officialResult = this.selectedResultByMatchId()[match.id];
		if (!officialResult) {
			return;
		}

		this.errorMessage.set("");
		this.successMessage.set("");
		this.submittingMatchId.set(match.id);
		this.matchesService.recordMatchResult(match.id, { officialResult }).subscribe({
			next: (result) => {
				this.successMessage.set(`Recorded ${match.homeTeamName} vs ${match.awayTeamName}. Jackpot is now ${formatCopaCoin(result.championJackpotCc)} CC.`);
				this.submittingMatchId.set(null);
				this.loadData();
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? "Unable to record result.");
				this.submittingMatchId.set(null);
			},
		});
	}

	settleChampion(): void {
		if (!this.selectedChampionTeamName()) {
			return;
		}

		this.errorMessage.set("");
		this.successMessage.set("");
		this.isSettlingChampion.set(true);
		this.matchesService.settleChampion({ championTeamName: this.selectedChampionTeamName() }).subscribe({
			next: (result) => {
				this.successMessage.set(`Champion settled for ${result.championTeamName}. Winners: ${result.winnersCount}. Payout: ${formatCopaCoin(result.totalPayoutPerWinnerCc)} CC.`);
				this.isSettlingChampion.set(false);
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? "Unable to settle champion bets.");
				this.isSettlingChampion.set(false);
			},
		});
	}

	syncFootballData(): void {
		this.errorMessage.set("");
		this.successMessage.set("");
		this.isSyncingFootballData.set(true);
		this.matchesService.syncFootballData().subscribe({
			next: (result) => {
				this.successMessage.set(`Synced ${result.matchesCount} matches, ${result.teamsCount} teams, ${result.groupsCount} groups from ${result.providerName}.`);
				this.isSyncingFootballData.set(false);
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? "Unable to sync external football data.");
				this.isSyncingFootballData.set(false);
			},
		});
	}

	syncPlayerSquads(): void {
		this.errorMessage.set("");
		this.successMessage.set("");
		this.isSyncingPlayers.set(true);
		this.matchesService.syncPlayerSquads().subscribe({
			next: (result) => {
				if (result.notConfigured) {
					this.successMessage.set("Player squad sync is not configured. Set ApiSportsFootball__ApiKey to enable it.");
					this.isSyncingPlayers.set(false);
					return;
				}

				const errorSummary = result.errors.length > 0
					? ` ${result.errors.length} team(s) reported errors: ${result.errors.map((error) => `${error.teamName} (${error.rateLimited ? "rate limited" : error.message})`).join(", ")}.`
					: "";
				this.successMessage.set(`Synced ${result.playersIndexedCount} players across ${result.teamsProcessedCount} teams from ${result.providerName}.${errorSummary}`);
				this.lastPlayerSyncAtUtc.set(result.syncedAtUtc);
				this.adminService.rememberLastPlayerSyncAtUtc(result.syncedAtUtc);
				this.isSyncingPlayers.set(false);
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? "Unable to sync player squads.");
				this.isSyncingPlayers.set(false);
			},
		});
	}

	importGroupStageFixtures(): void {
		this.errorMessage.set("");
		this.successMessage.set("");
		this.isImportingFixtures.set(true);
		this.matchesService.importGroupStageFixtures().subscribe({
			next: (result) => {
				const unsafeSkipMessage = result.unsafeUpdateSkippedCount > 0
					? ` ${result.unsafeUpdateSkippedCount} fixture updates were blocked because bets already exist.`
					: "";
				this.successMessage.set(`Imported ${result.importedCount}, updated ${result.updatedCount}, skipped ${result.skippedCount} group stage fixtures.${unsafeSkipMessage}`);
				this.isImportingFixtures.set(false);
				this.loadData();
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? "Unable to import group stage fixtures.");
				this.isImportingFixtures.set(false);
			},
		});
	}

	createInvitation(email: string, roleName: string): void {
		const request = this.createInvitationRequest(email, roleName);
		if (!request) {
			this.errorMessage.set("Enter a valid invited email.");
			return;
		}

		this.errorMessage.set("");
		this.successMessage.set("");
		this.isCreatingInvitation.set(true);
		this.adminService.createInvitation(request).subscribe({
			next: (result) => {
				const action = result.wasAlreadyInvited ? "was already invited" : "is invited";
				this.successMessage.set(`${result.email} ${action} as ${result.roleName}.`);
				this.isCreatingInvitation.set(false);
			},
			error: (error: { error?: { error?: string; detail?: string } }) => {
				this.errorMessage.set(error.error?.error ?? error.error?.detail ?? "Unable to create invitation.");
				this.isCreatingInvitation.set(false);
			},
		});
	}

	private isSelection(selection: string): selection is MatchBetSelection {
		return selection === "Home" || selection === "Draw" || selection === "Away";
	}

	private createInvitationRequest(email: string, roleName: string): CreateUserInvitationRequest | null {
		const normalizedEmail = email.trim();
		if (!normalizedEmail) {
			return null;
		}

		return {
			email: normalizedEmail,
			roleName: roleName === "Admin" ? "Admin" : "Bettor",
		};
	}
}
