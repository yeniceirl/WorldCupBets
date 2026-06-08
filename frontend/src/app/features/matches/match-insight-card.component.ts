import { Component, inject, input, OnInit, signal } from "@angular/core";
import { MatchesService } from "./matches.service";
import type { MatchInsights } from "./matches.models";

@Component({
	selector: "app-match-insight-card",
	template: `
		@if (isLoading()) {
			<aside class="mt-4 animate-pulse rounded-2xl border border-slate-200 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-900/70" data-testid="match-insights-loading">
				<div class="h-3 w-32 rounded-full bg-slate-200 dark:bg-slate-700"></div>
				<div class="mt-3 h-2.5 w-full rounded-full bg-slate-200 dark:bg-slate-800"></div>
				<div class="mt-2 h-2.5 w-5/6 rounded-full bg-slate-200 dark:bg-slate-800"></div>
			</aside>
		}

		@if (insights(); as matchInsights) {
			@if (matchInsights.isAvailable) {
				<aside class="mt-4 rounded-2xl border border-violet-200 bg-violet-50/70 p-4 dark:border-violet-900 dark:bg-violet-950/40" data-testid="match-insights-panel">
					<p class="text-[0.65rem] font-black uppercase tracking-[0.2em] text-violet-700 dark:text-violet-300">AI-generated insights</p>

					@if (matchInsights.facts.length > 0) {
						<div class="mt-3">
							<p class="text-xs font-bold uppercase tracking-wide text-slate-600 dark:text-slate-300">Did you know?</p>
							<ul class="mt-1 grid gap-1 text-sm text-slate-700 dark:text-slate-200">
								@for (fact of matchInsights.facts; track $index) {
									<li class="flex gap-2">
										<span aria-hidden="true">•</span>
										<span>{{ fact.text }}</span>
									</li>
								}
							</ul>
						</div>
					}

					@if (matchInsights.antecedents.length > 0) {
						<div class="mt-3">
							<p class="text-xs font-bold uppercase tracking-wide text-slate-600 dark:text-slate-300">Head-to-head</p>
							<ul class="mt-1 grid gap-1 text-sm text-slate-700 dark:text-slate-200">
								@for (antecedent of matchInsights.antecedents; track $index) {
									<li class="flex gap-2">
										<span aria-hidden="true">•</span>
										<span>{{ antecedent.text }}</span>
									</li>
								}
							</ul>
						</div>
					}

					@if (matchInsights.qa.length > 0) {
						<div class="mt-3 grid gap-2">
							<p class="text-xs font-bold uppercase tracking-wide text-slate-600 dark:text-slate-300">Quick Q&amp;A</p>
							@for (qaPair of matchInsights.qa; track $index) {
								<div class="rounded-xl bg-white/70 px-3 py-2 text-sm dark:bg-slate-950/60">
									<p class="font-semibold text-slate-900 dark:text-white">{{ qaPair.question }}</p>
									<p class="mt-1 text-slate-700 dark:text-slate-200">{{ qaPair.answer }}</p>
								</div>
							}
						</div>
					}
				</aside>
			}
		}
	`,
})
export class MatchInsightCardComponent implements OnInit {
	private readonly matchesService = inject(MatchesService);

	readonly matchId = input.required<number>();

	readonly isLoading = signal(true);
	readonly insights = signal<MatchInsights | null>(null);

	ngOnInit(): void {
		this.matchesService.getMatchInsights(this.matchId()).subscribe({
			next: (matchInsights) => {
				this.insights.set(matchInsights);
				this.isLoading.set(false);
			},
			error: () => {
				this.insights.set(null);
				this.isLoading.set(false);
			},
		});
	}
}
