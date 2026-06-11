import { Component, inject } from "@angular/core";
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { AuthService } from "./core/auth/auth.service";
import { AuthStateService } from "./core/auth/auth-state.service";
import { ThemeService } from "./core/theme/theme.service";

@Component({
  selector: "app-root",
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen overflow-hidden text-slate-950 dark:text-slate-100">
      <header class="sticky top-0 z-10 border-b border-white/60 bg-white/80 shadow-sm backdrop-blur-xl dark:border-slate-700/70 dark:bg-slate-950/80">
        <div class="mx-auto flex max-w-6xl items-center justify-between gap-4 px-6 py-4">
          <a class="group flex items-center gap-3" routerLink="/matches">
            <span class="grid size-11 place-items-center rounded-2xl bg-gradient-to-br from-amber-300 via-sky-400 to-emerald-400 text-sm font-black shadow-lg shadow-sky-500/20 transition group-hover:rotate-6">CC</span>
            <span>
              <span class="block text-lg font-black tracking-tight">WorldCupBets</span>
              <span class="block text-xs font-medium uppercase tracking-[0.22em] text-slate-500 dark:text-slate-400">CopaCoin 2026</span>
            </span>
          </a>

          <div class="flex items-center gap-3">
            <nav class="hidden flex-wrap items-center gap-2 text-sm font-medium md:flex">
              @if (!authState.isAuthenticated()) {
                <a class="rounded-full px-3 py-2 text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/auth/login" routerLinkActive="bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-200">Login</a>
              } @else {
                <a class="rounded-full px-3 py-2 text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/matches" routerLinkActive="bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-200">Matches</a>
                <a class="rounded-full px-3 py-2 text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/bets" routerLinkActive="bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-200">My Bets</a>
                <a class="rounded-full px-3 py-2 text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/challenges" routerLinkActive="bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-200">Challenges</a>
                <a class="rounded-full px-3 py-2 text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/leaderboard" routerLinkActive="bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-200">Leaderboard</a>
                @if (isAdmin()) {
                  <a class="rounded-full px-3 py-2 text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/admin" routerLinkActive="bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-200">Admin</a>
                }
              }
            </nav>

            @if (authState.user(); as user) {
              <details class="group relative">
                <summary class="flex cursor-pointer list-none items-center gap-2 rounded-full border border-slate-200 bg-white/85 px-2 py-2 text-sm font-semibold text-slate-800 shadow-sm transition hover:border-sky-300 dark:border-slate-700 dark:bg-slate-900/85 dark:text-slate-100">
                  <span class="grid size-8 place-items-center rounded-full bg-gradient-to-br from-sky-300 to-emerald-300 text-xs font-black text-slate-950">{{ getInitials(user.displayName) }}</span>
                  <span class="hidden max-w-32 truncate lg:block">{{ user.displayName }}</span>
                </summary>
                <div class="absolute right-0 mt-3 w-64 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl shadow-slate-900/15 dark:border-slate-700 dark:bg-slate-950">
                  <div class="border-b border-slate-100 p-4 dark:border-slate-800">
                    <p class="font-bold text-slate-950 dark:text-white">{{ user.displayName }}</p>
                    <p class="mt-1 truncate text-xs text-slate-500 dark:text-slate-400">{{ user.email }}</p>
                    <p class="mt-2 text-xs font-semibold uppercase tracking-[0.18em] text-sky-700 dark:text-sky-300">{{ user.roles.join(" · ") }}</p>
                  </div>
                  <nav class="grid p-2 text-sm font-medium md:hidden">
                    <a class="rounded-xl px-3 py-2 text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/matches">Matches</a>
                    <a class="rounded-xl px-3 py-2 text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/bets">My Bets</a>
                    <a class="rounded-xl px-3 py-2 text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/challenges">Challenges</a>
                    <a class="rounded-xl px-3 py-2 text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/leaderboard">Leaderboard</a>
                    @if (isAdmin()) {
                      <a class="rounded-xl px-3 py-2 text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" routerLink="/admin">Admin</a>
                    }
                  </nav>
                  <div class="grid gap-1 border-t border-slate-100 p-2 text-sm dark:border-slate-800">
                    <button type="button" class="rounded-xl px-3 py-2 text-left font-medium text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800" (click)="themeService.toggle()">
                      Switch to {{ themeService.isDark() ? "light" : "dark" }} mode
                    </button>
                    <button type="button" class="rounded-xl px-3 py-2 text-left font-medium text-rose-700 transition hover:bg-rose-50 dark:text-rose-200 dark:hover:bg-rose-950" (click)="logout()" data-testid="logout-button">
                      Logout
                    </button>
                  </div>
                </div>
              </details>
            } @else {
              <button type="button" class="rounded-full border border-slate-200 bg-white/80 px-3 py-2 text-sm font-medium text-slate-700 shadow-sm transition hover:border-sky-300 dark:border-slate-700 dark:bg-slate-900/80 dark:text-slate-200" (click)="themeService.toggle()">
                {{ themeService.isDark() ? "Light" : "Dark" }} mode
              </button>
            }
          </div>
        </div>
      </header>

      <main class="mx-auto max-w-6xl px-6 py-8">
        <router-outlet />
      </main>
    </div>
  `
})
export class AppComponent {
  readonly authState = inject(AuthStateService);
  readonly themeService = inject(ThemeService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  isAdmin(): boolean {
    return this.authState.user()?.roles.includes("Admin") ?? false;
  }

  getInitials(displayName: string): string {
    return displayName
      .split(" ")
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase())
      .join("") || "CC";
  }

  logout(): void {
    this.authService.signOut();
    void this.router.navigateByUrl("/auth/login");
  }
}
