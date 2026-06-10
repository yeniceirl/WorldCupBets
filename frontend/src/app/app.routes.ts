import type { Routes } from "@angular/router";
import { authGuard } from "./core/auth/auth.guard";

export const appRoutes: Routes = [
	{
		path: "",
		pathMatch: "full",
		redirectTo: "auth/login",
	},
	{
		path: "auth",
		pathMatch: "full",
		redirectTo: "auth/login",
	},
	{
		path: "auth/login",
		loadComponent: () =>
			import("./auth/login-page.component").then((m) => m.LoginPageComponent),
	},
	{
		path: "auth/callback",
		loadComponent: () =>
			import("./auth/login-callback-page.component").then(
				(m) => m.LoginCallbackPageComponent,
			),
	},
	{
		path: "matches",
		canActivate: [authGuard],
		loadComponent: () =>
			import("./features/matches/matches-page.component").then(
				(m) => m.MatchesPageComponent,
			),
	},
	{
		path: "bets",
		canActivate: [authGuard],
		loadComponent: () =>
			import("./features/bets/bets-page.component").then(
				(m) => m.BetsPageComponent,
			),
	},
	{
		path: "challenges",
		canActivate: [authGuard],
		loadComponent: () =>
			import("./features/challenges/challenges-page.component").then(
				(m) => m.ChallengesPageComponent,
			),
	},
	{
		path: "leaderboard",
		canActivate: [authGuard],
		loadComponent: () =>
			import("./features/leaderboard/leaderboard-page.component").then(
				(m) => m.LeaderboardPageComponent,
			),
	},
	{
		path: "admin",
		canActivate: [authGuard],
		loadComponent: () =>
			import("./features/admin/admin-page.component").then(
				(m) => m.AdminPageComponent,
			),
	},
	{
		path: "**",
		redirectTo: "auth/login",
	},
];
