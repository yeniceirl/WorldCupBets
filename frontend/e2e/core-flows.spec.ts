import { expect, test, type Page } from "@playwright/test";

const userSummary = {
	id: 1,
	displayName: "Ada Lovelace",
	email: "ada@example.com",
	currentBalanceCc: 1000,
	rescueCount: 0,
	rescueDebtCc: 0,
};

const championMarket = {
	teamOptions: ["Argentina", "Japan"],
	stakeAmountCc: 50,
	bettingClosesAtUtc: "2026-06-28T18:00:00Z",
	isBettingOpen: true,
	isSettled: false,
	currentUserChampionTeamName: null,
};

const specialMarket = {
	stakeAmountCc: 50,
	bettingClosesAtUtc: "2026-06-28T18:00:00Z",
	isBettingOpen: true,
	playerBets: [],
};

const matches = [
	{
		id: 10,
		stage: "Group stage",
		homeTeamName: "Argentina",
		awayTeamName: "Japan",
		groupName: "J",
		startsAtUtc: "2026-06-12T18:00:00Z",
		bettingClosesAtUtc: "2026-06-12T18:00:00Z",
		isBettingOpen: true,
		stakeAmountCc: 5,
		venue: "MetLife Stadium",
		currentUserBetSelection: null,
		officialResult: null,
		isSettled: false,
		settledAtUtc: null,
	},
];

const footballDataSnapshot = {
	teams: [
		{ externalId: "37", nameEn: "Argentina", fifaCode: "ARG", iso2: "AR", groupName: "J", flagUrl: null },
		{ externalId: "12", nameEn: "Japan", fifaCode: "JPN", iso2: "JP", groupName: "J", flagUrl: null },
	],
	stadiums: [],
	groupStandings: [],
	matches: [],
	syncedAtUtc: "2026-06-01T00:00:00Z",
};

test.beforeEach(async ({ page }) => {
	await page.addInitScript(() => {
		localStorage.setItem("worldcupbets.auth.accessToken", "test-token");
		localStorage.setItem("worldcupbets.auth.user", JSON.stringify({
			id: 1,
			email: "ada@example.com",
			displayName: "Ada Lovelace",
			roles: ["Bettor"],
		}));
	});

	await mockApi(page);
});

test("matches page focuses on match bets without champion card", async ({ page }) => {
	await page.goto("/matches");

	await expect(page.getByRole("heading", { name: "Upcoming matches" })).toBeVisible();
	await expect(page.getByTestId("champion-market-card")).toHaveCount(0);
	await expect(page.getByText("Argentina vs Japan")).toBeVisible();

	await page.getByRole("button", { name: "Argentina" }).click();

	await expect(page.getByTestId("success-message")).toContainText("Bet placed for Argentina");
	await expect(page.getByTestId("match-current-pick-10")).toContainText("Argentina");
});

test("my bets can place champion, best player, and top scorer picks", async ({ page }) => {
	await page.goto("/bets");

	await expect(page.getByRole("heading", { name: "Your CopaCoin ticket book" })).toBeVisible();
	await page.getByTestId("champion-team-select").selectOption("Argentina");
	await page.getByTestId("place-champion-bet-button").click();
	await expect(page.getByTestId("champion-market-card")).toContainText("Argentina");

	await page.getByPlaceholder("Type at least 3 characters").first().fill("Lio");
	await page.getByRole("button", { name: /Lionel Messi/ }).click();
	await page.getByTestId("place-special-player-bet-BestPlayer").click();
	await expect(page.getByTestId("special-player-bet-BestPlayer")).toContainText("Lionel Messi");

	await page.getByPlaceholder("Type at least 3 characters").first().fill("Kyl");
	await page.getByRole("button", { name: /Kylian Mbappe/ }).click();
	await page.getByTestId("place-special-player-bet-TopScorer").click();
	await expect(page.getByTestId("special-player-bet-TopScorer")).toContainText("Kylian Mbappe");
	await expect(page.getByText("3/3")).toBeVisible();
});

test.describe("admin player squad sync", () => {
	test.beforeEach(async ({ page }) => {
		await page.addInitScript(() => {
			localStorage.setItem("worldcupbets.auth.accessToken", "test-admin-token");
			localStorage.setItem("worldcupbets.auth.user", JSON.stringify({
				id: 2,
				email: "admin@example.com",
				displayName: "Ada Admin",
				roles: ["Admin"],
			}));
		});

		await mockApi(page);
		await page.route("**/api/football-data/players/sync", async (route) => route.fulfill({
			json: {
				providerName: "api-sports",
				notConfigured: false,
				teamsProcessedCount: 2,
				playersIndexedCount: 46,
				errors: [{ teamName: "Japan", message: "team not found", rateLimited: false }],
				syncedAtUtc: "2026-06-07T12:00:00Z",
			},
		}));
	});

	test("admin can trigger player squad sync and see results persist across reload", async ({ page }) => {
		await page.goto("/admin");

		await expect(page.getByRole("heading", { name: "Sync player squads" })).toBeVisible();
		await page.getByTestId("admin-sync-players").click();

		await expect(page.getByTestId("success-message")).toContainText("Synced 46 players across 2 teams from api-sports");
		await expect(page.getByTestId("success-message")).toContainText("Japan (team not found)");
		await expect(page.getByTestId("admin-players-last-synced")).toContainText("Last synced");

		await page.reload();

		await expect(page.getByTestId("admin-players-last-synced")).toContainText("Last synced");
	});
});

async function mockApi(page: Page): Promise<void> {
	await page.route("**/api/me/summary", async (route) => route.fulfill({ json: userSummary }));
	await page.route("**/api/bets/champion", async (route) => {
		if (route.request().method() === "POST") {
			return route.fulfill({ json: { teamName: "Argentina", stakeAmountCc: 50, remainingBalanceCc: 950, placedAtUtc: "2026-06-01T00:00:00Z" } });
		}

		return route.fulfill({ json: championMarket });
	});
	await page.route("**/api/bets/special", async (route) => route.fulfill({ json: specialMarket }));
	await page.route("**/api/bets/special/player", async (route) => {
		const request = route.request().postDataJSON() as { category: "BestPlayer" | "TopScorer"; playerName: string; externalPlayerId: string | null };
		return route.fulfill({
			json: {
				category: request.category,
				playerName: request.playerName,
				externalPlayerId: request.externalPlayerId,
				stakeAmountCc: 50,
				remainingBalanceCc: 900,
				placedAtUtc: "2026-06-01T00:00:00Z",
			},
		});
	});
	await page.route("**/api/bets/matches", async (route) => route.fulfill({ json: { matchId: 10, selection: "Home", stakeAmountCc: 5, remainingBalanceCc: 995, placedAtUtc: "2026-06-01T00:00:00Z" } }));
	await page.route("**/api/matches", async (route) => route.fulfill({ json: matches }));
	await page.route("**/api/football-data/snapshot", async (route) => route.fulfill({ json: footballDataSnapshot }));
	await page.route("**/api/football-data/players/search**", async (route) => route.fulfill({
		json: [
			{ externalId: "34146370", name: "Lionel Messi", teamName: "Inter Miami", nationality: "Argentina", position: "Forward", thumbnailUrl: null },
			{ externalId: "34161330", name: "Kylian Mbappe", teamName: "Real Madrid", nationality: "France", position: "Forward", thumbnailUrl: null },
		],
	}));
}
