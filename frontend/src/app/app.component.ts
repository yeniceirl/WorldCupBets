import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen bg-slate-50 text-slate-900">
      <header class="border-b border-slate-200 bg-white">
        <div class="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <a class="text-lg font-semibold" routerLink="/">WorldCupBets</a>
          <nav class="flex flex-wrap gap-3 text-sm">
            <a routerLink="/auth/login" routerLinkActive="font-semibold">Login</a>
            <a routerLink="/matches" routerLinkActive="font-semibold">Matches</a>
            <a routerLink="/bets" routerLinkActive="font-semibold">Bets</a>
            <a routerLink="/leaderboard" routerLinkActive="font-semibold">Leaderboard</a>
            <a routerLink="/admin" routerLinkActive="font-semibold">Admin</a>
          </nav>
        </div>
      </header>

      <main class="mx-auto max-w-6xl px-6 py-8">
        <router-outlet />
      </main>
    </div>
  `
})
export class AppComponent {}
