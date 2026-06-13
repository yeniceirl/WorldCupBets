import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { formatCopaCoin } from "../../shared/copa-coin-format";
import { AdminService } from "./admin.service";
import type { AuditBalanceSummary, AuditLedgerItem, AuditUserSubledger } from "./admin.models";

@Component({
	selector: "app-audit-page",
	imports: [DatePipe],
	template: `
		<section class="space-y-6">
			<header class="overflow-hidden rounded-[2rem] border border-white/70 bg-white/85 p-6 shadow-xl shadow-sky-900/5 backdrop-blur dark:border-slate-700 dark:bg-slate-950/80">
				<div class="grid gap-5 sm:grid-cols-[1fr_auto] sm:items-center">
					<div>
						<p class="text-sm font-bold uppercase tracking-[0.24em] text-fuchsia-700 dark:text-fuchsia-300">Audit room</p>
						<h1 class="mt-2 text-4xl font-black tracking-tight text-slate-950 dark:text-white">Derived balance board</h1>
						<p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600 dark:text-slate-300">See the hobby-league ledger and subledger: available balance, pending exposure, won and lost totals, plus the unresolved reason behind every pending bet.</p>
					</div>
					<div class="rounded-3xl bg-gradient-to-br from-fuchsia-500 via-sky-500 to-emerald-400 px-6 py-8 text-white shadow-2xl shadow-fuchsia-500/20">
						<p class="text-xs font-black uppercase tracking-[0.2em] opacity-80">For all bettors</p>
						<p class="mt-2 text-3xl font-black">Ledger + subledger</p>
					</div>
				</div>
			</header>

			@if (errorMessage()) {
				<section class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-200">{{ errorMessage() }}</section>
			}

			@if (auditSummary(); as audit) {
				<section class="rounded-[2rem] border border-slate-200 bg-white/90 p-5 shadow-sm dark:border-slate-700 dark:bg-slate-950/80">
					<div class="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
						<div>
							<p class="text-xs font-bold uppercase tracking-[0.2em] text-fuchsia-700 dark:text-fuchsia-300">Audit ledger</p>
							<h2 class="mt-2 text-2xl font-black text-slate-950 dark:text-white">Current-state balance report</h2>
						</div>
						<button type="button" class="rounded-full border border-slate-200 px-3 py-2 text-sm font-medium text-slate-700 transition hover:border-fuchsia-300 dark:border-slate-700 dark:text-slate-200" (click)="loadAuditSummary()">Refresh audit</button>
					</div>

					<div class="mt-4 rounded-2xl border border-fuchsia-200 bg-fuchsia-50/70 p-4 text-sm text-fuchsia-900 dark:border-fuchsia-900/70 dark:bg-fuchsia-950/40 dark:text-fuchsia-100">
						<p class="font-bold tracking-wide">{{ audit.metadata.label }}</p>
						<p class="mt-1 text-xs leading-5 opacity-80">{{ audit.metadata.description }}</p>
					</div>

					@if (isLoadingAuditSummary()) {
						<p class="mt-5 text-sm text-slate-600 dark:text-slate-300">Loading audit balances...</p>
					} @else if (audit.rows.length === 0) {
						<p class="mt-5 rounded-xl border border-dashed border-slate-300 p-4 text-sm text-slate-600 dark:border-slate-700 dark:text-slate-300">No audit rows yet. Once people place bets or get settled, the board will populate here.</p>
					} @else {
						<div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-800">
							<div class="overflow-x-auto">
								<table class="min-w-full table-fixed divide-y divide-slate-200 text-sm dark:divide-slate-800">
									<thead class="bg-slate-50 text-left text-xs font-bold uppercase tracking-[0.18em] text-slate-500 dark:bg-slate-900 dark:text-slate-400">
										<tr>
											<th class="w-[28%] px-4 py-3">User</th>
											<th class="w-[14.4%] px-4 py-3 text-right">Available</th>
											<th class="w-[14.4%] px-4 py-3 text-right">Pending</th>
											<th class="w-[14.4%] px-4 py-3 text-right">Derived total</th>
											<th class="w-[14.4%] px-4 py-3 text-right">Won</th>
											<th class="w-[14.4%] px-4 py-3 text-right">Lost</th>
										</tr>
									</thead>
									<tbody class="divide-y divide-slate-100 dark:divide-slate-900">
										@for (row of audit.rows; track row.userId) {
											<tr class="cursor-pointer transition hover:bg-fuchsia-50/80 dark:hover:bg-fuchsia-950/20" [class.bg-fuchsia-50]="selectedAuditUserId() === row.userId" [class.dark:bg-fuchsia-950/30]="selectedAuditUserId() === row.userId" (click)="toggleAuditUser(row.userId)">
													<td class="px-4 py-4 align-top">
														<div class="font-bold text-slate-950 dark:text-white">{{ row.displayName }}</div>
														<div class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ row.email }}</div>
													</td>
													<td class="px-4 py-4 text-right font-semibold text-slate-800 dark:text-slate-100">{{ formatCopaCoin(row.availableBalanceCc) }}</td>
													<td class="px-4 py-4 text-right font-semibold text-amber-700 dark:text-amber-300">{{ formatCopaCoin(row.pendingTotalCc) }}</td>
													<td class="px-4 py-4 text-right font-black text-slate-950 dark:text-white">{{ formatCopaCoin(row.derivedTotalBalanceCc) }}</td>
													<td class="px-4 py-4 text-right font-semibold text-emerald-700 dark:text-emerald-300">{{ formatCopaCoin(row.wonTotalCc) }}</td>
												<td class="px-4 py-4 text-right font-semibold text-rose-700 dark:text-rose-300">{{ formatCopaCoin(row.lostTotalCc) }}</td>
											</tr>
											@if (selectedAuditUserId() === row.userId) {
												<tr>
													<td colspan="6" class="bg-fuchsia-50/55 px-4 py-4 dark:bg-fuchsia-950/15">
														@if (isLoadingAuditDetail()) {
															<div class="rounded-2xl border border-dashed border-fuchsia-300 bg-white/70 p-5 text-sm text-slate-600 dark:border-fuchsia-800 dark:bg-slate-950/60 dark:text-slate-300">Loading subledger...</div>
														} @else if (auditDetail(); as detail) {
															<div class="space-y-4 rounded-2xl border border-fuchsia-200 bg-white/90 p-5 shadow-sm dark:border-fuchsia-900/70 dark:bg-slate-950/80">
																<div class="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
																	<div>
																		<p class="text-xs font-bold uppercase tracking-[0.2em] text-fuchsia-700 dark:text-fuchsia-300">Audit subledger</p>
																		<h3 class="mt-2 text-2xl font-black text-slate-950 dark:text-white">{{ detail.user.displayName }}</h3>
																		<p class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ detail.user.email }}</p>
																	</div>
																	<div class="grid gap-2 sm:grid-cols-4">
																		<div class="rounded-xl bg-slate-50 px-3 py-3 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Available</p><p class="mt-1 text-lg font-black text-slate-950 dark:text-white">{{ formatCopaCoin(detail.user.availableBalanceCc) }}</p></div>
																		<div class="rounded-xl bg-slate-50 px-3 py-3 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Pending</p><p class="mt-1 text-lg font-black text-amber-700 dark:text-amber-300">{{ formatCopaCoin(detail.user.pendingTotalCc) }}</p></div>
																		<div class="rounded-xl bg-slate-50 px-3 py-3 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Won</p><p class="mt-1 text-lg font-black text-emerald-700 dark:text-emerald-300">{{ formatCopaCoin(detail.user.wonTotalCc) }}</p></div>
																		<div class="rounded-xl bg-slate-50 px-3 py-3 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Lost</p><p class="mt-1 text-lg font-black text-rose-700 dark:text-rose-300">{{ formatCopaCoin(detail.user.lostTotalCc) }}</p></div>
																	</div>
																</div>

																<div class="grid gap-4 xl:grid-cols-[0.9fr_1.1fr]">
																	<section class="rounded-2xl border border-slate-200 bg-slate-50/70 p-4 dark:border-slate-800 dark:bg-slate-900/40">
																		<div class="flex items-center justify-between gap-3">
																			<div>
																				<p class="text-xs font-bold uppercase tracking-[0.18em] text-sky-700 dark:text-sky-300">Tournament picks</p>
																				<h4 class="mt-1 text-lg font-black text-slate-950 dark:text-white">Big-picture bets</h4>
																			</div>
																			<span class="rounded-full bg-sky-100 px-3 py-1 text-xs font-bold text-sky-800 dark:bg-sky-950 dark:text-sky-200">{{ tournamentPickItems().length }} items</span>
																		</div>
																		@if (tournamentPickItems().length === 0) {
																			<p class="mt-4 rounded-xl border border-dashed border-slate-300 p-4 text-sm text-slate-600 dark:border-slate-700 dark:text-slate-300">No tournament picks recorded for this bettor.</p>
																		} @else {
																			<div class="mt-4 grid gap-3">
																				@for (item of tournamentPickItems(); track item.sourceType + '-' + item.sourceId) {
																					<article class="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-950">
																						<div class="flex items-start justify-between gap-3">
																						<div>
																							<p class="text-[11px] font-bold uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">{{ item.label }}</p>
																							<p class="mt-1 text-sm font-bold text-slate-950 dark:text-white">{{ tournamentPickSelection(item) }}</p>
																							<p class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ item.placedAtUtc | date: "mediumDate" : "UTC" }}</p>
																						</div>
																						<span class="rounded-full px-3 py-1 text-[11px] font-black uppercase tracking-[0.16em]" [class]="auditStatusClass(item)">{{ auditStatusLabel(item.status) }}</span>
																					</div>
																					<div class="mt-3 grid gap-2 sm:grid-cols-3">
																						<div class="rounded-lg bg-slate-50 px-3 py-2 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Stake</p><p class="mt-1 font-bold text-slate-900 dark:text-slate-100">{{ formatCopaCoin(item.stakeAmountCc) }}</p></div>
																						<div class="rounded-lg bg-slate-50 px-3 py-2 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Credit</p><p class="mt-1 font-bold text-emerald-700 dark:text-emerald-300">{{ formatCopaCoin(item.creditAmountCc) }}</p></div>
																						<div class="rounded-lg bg-slate-50 px-3 py-2 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Pending</p><p class="mt-1 font-bold text-amber-700 dark:text-amber-300">{{ formatCopaCoin(item.pendingAmountCc) }}</p></div>
																					</div>
																					@if (item.pendingReason) {
																						<p class="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900 dark:border-amber-900/70 dark:bg-amber-950/40 dark:text-amber-100">{{ item.pendingReason }}</p>
																					}
																				</article>
																				}
																			</div>
																		}
																	</section>

																	<section class="rounded-2xl border border-slate-200 bg-slate-50/70 p-4 dark:border-slate-800 dark:bg-slate-900/40">
																		<div class="flex items-center justify-between gap-3">
																			<div>
																				<p class="text-xs font-bold uppercase tracking-[0.18em] text-emerald-700 dark:text-emerald-300">Match bets</p>
																				<h4 class="mt-1 text-lg font-black text-slate-950 dark:text-white">Pending first, history after</h4>
																			</div>
																			<span class="rounded-full bg-emerald-100 px-3 py-1 text-xs font-bold text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">{{ matchBetItems().length }} items</span>
																		</div>
																		@if (matchBetItems().length === 0) {
																			<p class="mt-4 rounded-xl border border-dashed border-slate-300 p-4 text-sm text-slate-600 dark:border-slate-700 dark:text-slate-300">No match bets recorded for this bettor.</p>
																		} @else {
																			<div class="mt-4 space-y-3">
																				@for (item of matchBetItems(); track item.sourceType + '-' + item.sourceId) {
																					<article class="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-950">
																						<div class="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
																							<div class="min-w-0">
																								<div class="flex flex-wrap items-center gap-2">
																									<p class="font-bold text-slate-950 dark:text-white">{{ item.label }}</p>
																									<span class="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-medium text-slate-700 dark:bg-slate-800 dark:text-slate-200">{{ matchBetSelection(item) }}</span>
																									<span class="rounded-full px-3 py-1 text-[11px] font-black uppercase tracking-[0.16em]" [class]="auditStatusClass(item)">{{ auditStatusLabel(item.status) }}</span>
																								</div>
																								<p class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ item.placedAtUtc | date: "medium" : "UTC" }} UTC</p>
																								@if (item.pendingReason) {
																									<p class="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900 dark:border-amber-900/70 dark:bg-amber-950/40 dark:text-amber-100">{{ item.pendingReason }}</p>
																								}
																							</div>
																							<div class="grid gap-2 sm:grid-cols-4 lg:min-w-[27rem]">
																								<div class="rounded-lg bg-slate-50 px-3 py-2 text-right dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Stake</p><p class="mt-1 font-bold text-slate-900 dark:text-slate-100">{{ formatCopaCoin(item.stakeAmountCc) }}</p></div>
																								<div class="rounded-lg bg-slate-50 px-3 py-2 text-right dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Credit</p><p class="mt-1 font-bold text-emerald-700 dark:text-emerald-300">{{ formatCopaCoin(item.creditAmountCc) }}</p></div>
																								<div class="rounded-lg bg-slate-50 px-3 py-2 text-right dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Loss</p><p class="mt-1 font-bold text-rose-700 dark:text-rose-300">{{ formatCopaCoin(item.lossAmountCc) }}</p></div>
																								<div class="rounded-lg bg-slate-50 px-3 py-2 text-right dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Pending</p><p class="mt-1 font-bold text-amber-700 dark:text-amber-300">{{ formatCopaCoin(item.pendingAmountCc) }}</p></div>
																							</div>
																						</div>
																						@if (item.metadata.length > 0) {
																							<div class="mt-3 flex flex-wrap gap-2">@for (meta of item.metadata; track meta.label + '-' + meta.value) {<span class="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-medium text-slate-700 dark:bg-slate-800 dark:text-slate-200">{{ meta.label }}: {{ meta.value }}</span>}</div>
																						}
																					</article>
																				}
																			</div>
																		}

																		@if (otherItems().length > 0) {
																			<div class="mt-5 border-t border-slate-200 pt-4 dark:border-slate-800">
																				<p class="text-xs font-bold uppercase tracking-[0.18em] text-violet-700 dark:text-violet-300">Other activity</p>
																				<div class="mt-3 space-y-3">
																					@for (item of otherItems(); track item.sourceType + '-' + item.sourceId) {
																						<article class="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-950">
																							<div class="flex items-start justify-between gap-3">
																								<div>
																									<p class="text-[11px] font-bold uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">{{ item.sourceType.replace('_', ' ') }}</p>
																									<h5 class="mt-1 font-bold text-slate-950 dark:text-white">{{ item.label }}</h5>
																								</div>
																								<span class="rounded-full px-3 py-1 text-[11px] font-black uppercase tracking-[0.16em]" [class]="auditStatusClass(item)">{{ auditStatusLabel(item.status) }}</span>
																							</div>
																						</article>
																					}
																				</div>
																			</div>
																		}
																	</section>
																</div>
															</div>
														}
													</td>
												</tr>
											}
										}
									</tbody>
								</table>
							</div>
						</div>
					}
				</section>
			} @else if (isLoadingAuditSummary()) {
				<section class="rounded-2xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-700 dark:border-sky-900 dark:bg-sky-950 dark:text-sky-200">Loading audit balances...</section>
			}
		</section>
	`,
})
export class AuditPageComponent {
	private readonly adminService = inject(AdminService);
	protected readonly formatCopaCoin = formatCopaCoin;

	readonly auditSummary = signal<AuditBalanceSummary | null>(null);
	readonly auditDetail = signal<AuditUserSubledger | null>(null);
	readonly isLoadingAuditSummary = signal(true);
	readonly isLoadingAuditDetail = signal(false);
	readonly selectedAuditUserId = signal<number | null>(null);
	readonly errorMessage = signal("");
	readonly tournamentPickItems = computed(() =>
		(this.auditDetail()?.items ?? []).filter((item) => item.sourceType === "tournament_pick").sort((left, right) => this.tournamentPickSortOrder(left) - this.tournamentPickSortOrder(right)),
	);
	readonly matchBetItems = computed(() =>
		(this.auditDetail()?.items ?? [])
			.filter((item) => item.sourceType === "match_bet")
			.sort((left, right) => this.matchBetSortOrder(left, right)),
	);
	readonly otherItems = computed(() =>
		(this.auditDetail()?.items ?? []).filter((item) => item.sourceType !== "tournament_pick" && item.sourceType !== "match_bet"),
	);

	constructor() {
		this.loadAuditSummary();
	}

	loadAuditSummary(): void {
		this.errorMessage.set("");
		this.isLoadingAuditSummary.set(true);
		this.adminService.getAuditBalanceSummary().subscribe({
			next: (auditSummary) => {
				this.auditSummary.set(auditSummary);
				this.isLoadingAuditSummary.set(false);
			},
			error: () => {
				this.errorMessage.set("Unable to load audit balances.");
				this.isLoadingAuditSummary.set(false);
			},
		});
	}

	toggleAuditUser(userId: number): void {
		if (this.selectedAuditUserId() === userId) {
			this.selectedAuditUserId.set(null);
			this.auditDetail.set(null);
			this.isLoadingAuditDetail.set(false);
			return;
		}

		this.selectedAuditUserId.set(userId);
		this.auditDetail.set(null);
		this.isLoadingAuditDetail.set(true);
		this.adminService.getAuditUserSubledger(userId).subscribe({
			next: (detail) => {
				if (this.selectedAuditUserId() !== userId) {
					return;
				}

				this.auditDetail.set(detail);
				this.isLoadingAuditDetail.set(false);
			},
			error: () => {
				if (this.selectedAuditUserId() !== userId) {
					return;
				}

				this.errorMessage.set("Unable to load audit subledger.");
				this.isLoadingAuditDetail.set(false);
			},
		});
	}

	auditStatusLabel(status: string): string {
		switch (status) {
			case "won":
				return "Won";
			case "lost":
				return "Lost";
			case "refunded":
				return "Refunded";
			default:
				return "Pending";
		}
	}

	auditStatusClass(item: AuditLedgerItem): string {
		switch (item.status) {
			case "won":
				return "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200";
			case "lost":
				return "bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-200";
			case "refunded":
				return "bg-slate-200 text-slate-800 dark:bg-slate-800 dark:text-slate-200";
			default:
				return "bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-200";
		}
	}

	tournamentPickSelection(item: AuditLedgerItem): string {
		return item.metadata.find((meta) => meta.label === "Selection")?.value ?? "No selection";
	}

	matchBetSelection(item: AuditLedgerItem): string {
		return item.metadata.find((meta) => meta.label === "Selection")?.value ?? "Unknown selection";
	}

	private tournamentPickSortOrder(item: AuditLedgerItem): number {
		switch (item.label) {
			case "Champion pick":
				return 0;
			case "Best player pick":
				return 1;
			case "Top scorer pick":
				return 2;
			default:
				return 3;
		}
	}

	private matchBetSortOrder(left: AuditLedgerItem, right: AuditLedgerItem): number {
		const leftPending = left.status === "pending" ? 0 : 1;
		const rightPending = right.status === "pending" ? 0 : 1;
		if (leftPending !== rightPending) {
			return leftPending - rightPending;
		}

		return new Date(right.placedAtUtc).getTime() - new Date(left.placedAtUtc).getTime();
	}
}
