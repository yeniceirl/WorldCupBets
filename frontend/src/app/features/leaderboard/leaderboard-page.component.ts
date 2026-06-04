import { Component, inject, signal } from "@angular/core";
import { LeaderboardService } from "./leaderboard.service";
import type { LeaderboardItem } from "./leaderboard.models";

@Component({
  selector: "app-leaderboard-page",
  template: `
    <section class="space-y-6">
      <header class="overflow-hidden rounded-[2rem] border border-white/70 bg-white/85 p-6 shadow-xl shadow-sky-900/5 backdrop-blur dark:border-slate-700 dark:bg-slate-950/80" data-testid="leaderboard-header">
        <div class="grid gap-6 lg:grid-cols-[1fr_auto] lg:items-center">
          <div>
            <p class="text-sm font-bold uppercase tracking-[0.24em] text-sky-700 dark:text-sky-300">CopaCoin leaderboard</p>
            <h1 class="mt-2 text-4xl font-black tracking-tight text-slate-950 dark:text-white">Current standings</h1>
            <p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600 dark:text-slate-300">Balances show settled CopaCoin only. Open bets stay visible as pending until the result is known.</p>
          </div>
          <img class="mx-auto h-36 w-36 object-contain drop-shadow-xl lg:mx-0" src="/assets/brand/leaderboard-mascot.webp" alt="CopaCoin leaderboard mascot" />
        </div>
      </header>

      @if (isLoading()) {
        <section class="rounded-2xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-700 dark:border-sky-900 dark:bg-sky-950 dark:text-sky-200" data-testid="leaderboard-loading">
          Loading leaderboard...
        </section>
      }

      @if (errorMessage()) {
        <section class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-200" data-testid="leaderboard-error">
          {{ errorMessage() }}
        </section>
      }

      @if (!isLoading() && !errorMessage() && leaderboard().length === 0) {
        <section class="grid gap-5 rounded-2xl border border-dashed border-slate-300 bg-white/80 p-8 text-sm text-slate-600 dark:border-slate-700 dark:bg-slate-950/60 dark:text-slate-300 sm:grid-cols-[1fr_auto] sm:items-center" data-testid="leaderboard-empty">
          <span>No leaderboard entries are available yet.</span>
          <img class="h-28 w-28 object-contain" src="/assets/brand/empty-state-mascot.webp" alt="Empty leaderboard mascot" />
        </section>
      }

      @if (leaderboard().length > 0) {
        <section class="overflow-hidden rounded-[2rem] border border-slate-200 bg-white/90 shadow-xl shadow-sky-900/5 dark:border-slate-700 dark:bg-slate-950/80" data-testid="leaderboard-list">
          @for (item of leaderboard(); track item.rank + '-' + item.displayName) {
            <article class="grid grid-cols-[auto_1fr_auto] items-center gap-4 border-b border-slate-100 px-5 py-4 last:border-b-0 dark:border-slate-800" [attr.data-testid]="'leaderboard-item-' + item.rank">
              <span class="flex size-11 items-center justify-center rounded-2xl text-sm font-black" [class]="getRankBadgeClasses(item.rank)">#{{ item.rank }}</span>
              <div>
                <h2 class="font-bold text-slate-950 dark:text-white">{{ item.displayName }}</h2>
                <p class="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">Current balance</p>
              </div>
              <div class="text-right">
                <p class="text-lg font-black text-slate-950 dark:text-white">{{ item.currentBalanceCc }} CC</p>
                @if (item.pendingStakeAmountCc > 0) {
                  <p class="mt-1 rounded-full bg-amber-50 px-3 py-1 text-xs font-bold text-amber-700 dark:bg-amber-950 dark:text-amber-200">{{ item.pendingStakeAmountCc }} CC pending</p>
                }
              </div>
            </article>
          }
        </section>
      }
    </section>
  `,
})
export class LeaderboardPageComponent {
  private readonly leaderboardService = inject(LeaderboardService);

  readonly leaderboard = signal<ReadonlyArray<LeaderboardItem>>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal("");

  constructor() {
    this.leaderboardService.getLeaderboard().subscribe({
      next: (leaderboard) => {
        this.leaderboard.set(leaderboard);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set("Unable to load the CopaCoin leaderboard right now.");
        this.isLoading.set(false);
      },
    });
  }

  getRankBadgeClasses(rank: number): string {
    if (rank === 1) {
      return "bg-amber-100 text-amber-800 dark:bg-amber-300 dark:text-slate-950";
    }

    if (rank === 2) {
      return "bg-sky-100 text-sky-800 dark:bg-sky-300 dark:text-slate-950";
    }

    if (rank === 3) {
      return "bg-emerald-100 text-emerald-800 dark:bg-emerald-300 dark:text-slate-950";
    }

    return "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200";
  }
}
