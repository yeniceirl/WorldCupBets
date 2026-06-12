import { DatePipe } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
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
						<p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600 dark:text-slate-300">See the hobby-league major and subledger: available balance, pending exposure, won and lost totals, plus the unresolved reason behind every pending bet.</p>
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
						<div class="mt-5 grid gap-5 xl:grid-cols-[1.35fr_0.95fr]">
							<div class="overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-800">
								<div class="overflow-x-auto">
									<table class="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-800">
										<thead class="bg-slate-50 text-left text-xs font-bold uppercase tracking-[0.18em] text-slate-500 dark:bg-slate-900 dark:text-slate-400">
											<tr>
												<th class="px-4 py-3">User</th>
												<th class="px-4 py-3 text-right">Available</th>
												<th class="px-4 py-3 text-right">Pending</th>
												<th class="px-4 py-3 text-right">Derived total</th>
												<th class="px-4 py-3 text-right">Won</th>
												<th class="px-4 py-3 text-right">Lost</th>
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
											}
										</tbody>
									</table>
								</div>
							</div>

							<aside class="rounded-2xl border border-slate-200 bg-slate-50/70 p-4 dark:border-slate-800 dark:bg-slate-900/40">
								@if (!selectedAuditUserId()) {
									<div class="rounded-xl border border-dashed border-slate-300 p-5 text-sm leading-6 text-slate-600 dark:border-slate-700 dark:text-slate-300">Pick a bettor from the mayor to inspect the subledger: wins, losses, refunds, and every pending dependency.</div>
								} @else if (isLoadingAuditDetail()) {
									<p class="text-sm text-slate-600 dark:text-slate-300">Loading subledger...</p>
								} @else if (auditDetail(); as detail) {
									<div>
									<p class="text-xs font-bold uppercase tracking-[0.2em] text-fuchsia-700 dark:text-fuchsia-300">Audit subledger</p>
										<h3 class="mt-2 text-2xl font-black text-slate-950 dark:text-white">{{ detail.user.displayName }}</h3>
										<p class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ detail.user.email }}</p>
										@if (detail.items.length === 0) {
											<p class="mt-4 rounded-xl border border-dashed border-slate-300 p-4 text-sm text-slate-600 dark:border-slate-700 dark:text-slate-300">No activity for this bettor yet.</p>
										} @else {
											<div class="mt-4 space-y-3">
												@for (item of detail.items; track item.sourceType + '-' + item.sourceId) {
													<article class="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-950">
														<div class="flex flex-col gap-3">
															<div class="flex items-start justify-between gap-3">
																<div>
																	<p class="text-[11px] font-bold uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">{{ item.sourceType.replace('_', ' ') }}</p>
																	<h4 class="mt-1 font-bold text-slate-950 dark:text-white">{{ item.label }}</h4>
																	<p class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ item.placedAtUtc | date: "medium" : "UTC" }} UTC</p>
																</div>
																<span class="rounded-full px-3 py-1 text-[11px] font-black uppercase tracking-[0.16em]" [class]="auditStatusClass(item)">{{ auditStatusLabel(item.status) }}</span>
															</div>
															<div class="grid gap-2 sm:grid-cols-2 xl:grid-cols-4">
																<div class="rounded-lg bg-slate-50 px-3 py-2 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Stake</p><p class="mt-1 font-bold text-slate-900 dark:text-slate-100">{{ formatCopaCoin(item.stakeAmountCc) }}</p></div>
																<div class="rounded-lg bg-slate-50 px-3 py-2 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Credit</p><p class="mt-1 font-bold text-emerald-700 dark:text-emerald-300">{{ formatCopaCoin(item.creditAmountCc) }}</p></div>
																<div class="rounded-lg bg-slate-50 px-3 py-2 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Loss</p><p class="mt-1 font-bold text-rose-700 dark:text-rose-300">{{ formatCopaCoin(item.lossAmountCc) }}</p></div>
																<div class="rounded-lg bg-slate-50 px-3 py-2 dark:bg-slate-900"><p class="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">Pending</p><p class="mt-1 font-bold text-amber-700 dark:text-amber-300">{{ formatCopaCoin(item.pendingAmountCc) }}</p></div>
															</div>
															@if (item.pendingReason) {
																<div class="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs leading-5 text-amber-900 dark:border-amber-900/70 dark:bg-amber-950/40 dark:text-amber-100"><span class="font-bold uppercase tracking-[0.14em]">Pending on</span><p class="mt-1">{{ item.pendingReason }}</p></div>
															}
															@if (item.metadata.length > 0) {
																<div class="flex flex-wrap gap-2">@for (meta of item.metadata; track meta.label + '-' + meta.value) {<span class="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-medium text-slate-700 dark:bg-slate-800 dark:text-slate-200">{{ meta.label }}: {{ meta.value }}</span>}</div>
															}
														</div>
													</article>
												}
											</div>
										}
									</div>
								}
							</aside>
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
}
