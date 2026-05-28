import type { Routes } from "@angular/router";

export const appRoutes: Routes = [
	{
		path: "",
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
		loadComponent: () =>
			import("./features/matches/matches-page.component").then(
				(m) => m.MatchesPageComponent,
			),
	},
	{
		path: "bets",
		loadComponent: () =>
			import("./features/bets/bets-page.component").then(
				(m) => m.BetsPageComponent,
			),
	},
	{
		path: "leaderboard",
		loadComponent: () =>
			import("./features/leaderboard/leaderboard-page.component").then(
				(m) => m.LeaderboardPageComponent,
			),
	},
	{
		path: "admin",
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
