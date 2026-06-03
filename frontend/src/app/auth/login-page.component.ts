import {
	type AfterViewInit,
	Component,
	DestroyRef,
	type ElementRef,
	ViewChild,
	inject,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Router } from "@angular/router";
import { AuthService } from "../core/auth/auth.service";
import { GoogleIdentityService } from "../core/auth/google-identity.service";
import { AuthStateService } from "../core/auth/auth-state.service";

declare global {
	interface Window {
		__env?: {
			googleClientId?: string;
			enableDevLogin?: boolean;
		};
	}
}

@Component({
	selector: "app-login-page",
	standalone: true,
	template: `
		<section class="mx-auto max-w-2xl rounded-[2rem] border border-white/70 bg-white/85 p-8 shadow-xl shadow-sky-900/5 backdrop-blur dark:border-slate-700 dark:bg-slate-950/80" data-testid="login-page">
			<p class="text-sm font-bold uppercase tracking-[0.24em] text-sky-700 dark:text-sky-300">Sign in</p>
			<h1 class="mt-2 text-4xl font-black tracking-tight text-slate-950 dark:text-white">Enter the CopaCoin arena</h1>
			<p class="mt-4 text-sm leading-6 text-slate-600 dark:text-slate-300">
				Use Google Identity Services to obtain an ID token, then exchange it with the backend.
			</p>

			@if (isLoading) {
				<p class="mt-6 rounded-xl border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-700 dark:border-sky-900 dark:bg-sky-950 dark:text-sky-200">
					Loading Google sign-in...
				</p>
			}

			@if (errorMessage) {
				<p class="mt-6 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-200">
					{{ errorMessage }}
				</p>
			}

			<div class="mt-6 flex flex-col gap-4">
				<div #googleButtonContainer class="min-h-10" data-testid="google-login-container"></div>
				@if (isDevLoginEnabled) {
					<div class="grid gap-3 rounded-2xl border border-dashed border-slate-300 bg-slate-50/80 p-4 dark:border-slate-700 dark:bg-slate-900/70 sm:grid-cols-2">
						<button
							type="button"
							class="rounded-xl border border-slate-300 bg-white px-4 py-3 text-left text-sm font-medium text-slate-800 transition hover:border-sky-400 hover:text-sky-700 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-100"
							(click)="signInWithDevLogin('Bettor')"
							[disabled]="isDevLoginLoading"
							data-testid="dev-login-bettor-button"
						>
							<span class="block font-bold">Dev Bettor</span>
							<span class="mt-1 block text-xs text-slate-500 dark:text-slate-400">Bet, check picks, and view standings.</span>
						</button>
						<button
							type="button"
							class="rounded-xl border border-slate-900 bg-slate-950 px-4 py-3 text-left text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60 dark:border-amber-300 dark:bg-amber-300 dark:text-slate-950"
							(click)="signInWithDevLogin('Admin')"
							[disabled]="isDevLoginLoading"
							data-testid="dev-login-admin-button"
						>
							<span class="block font-bold">Dev Admin</span>
							<span class="mt-1 block text-xs text-slate-300 dark:text-slate-800">Record results and settle champion bets.</span>
						</button>
					</div>
				}
				<p class="text-xs text-slate-500 dark:text-slate-400">
					The credential is kept in browser memory and forwarded to the callback route through router state,
					not the URL.
				</p>
			</div>
		</section>
	`,
})
export class LoginPageComponent implements AfterViewInit {
	private readonly destroyRef = inject(DestroyRef);
	private readonly router = inject(Router);
	private readonly authService = inject(AuthService);
	private readonly googleIdentityService = inject(GoogleIdentityService);
	private readonly authState = inject(AuthStateService);

	@ViewChild("googleButtonContainer", { static: true })
	private readonly googleButtonContainer?: ElementRef<HTMLDivElement>;

	isLoading = true;
	errorMessage = "";
	isDevLoginEnabled = window.__env?.enableDevLogin === true;
	isDevLoginLoading = false;

	constructor() {
		if (this.authState.isAuthenticated()) {
			void this.router.navigateByUrl("/matches");
		}
	}

	signInWithDevLogin(role: "Admin" | "Bettor"): void {
		this.errorMessage = "";
		this.isDevLoginLoading = true;

		this.authService.devLogin({
			role,
			displayName: role === "Admin" ? "Dev Admin" : "Dev Bettor",
			email: role === "Admin" ? "dev-admin@worldcupbets.local" : "dev-bettor@worldcupbets.local",
			googleSubject: role === "Admin" ? "dev-admin" : "dev-bettor",
		})
			.pipe(takeUntilDestroyed(this.destroyRef))
			.subscribe({
				next: () => {
					void this.router.navigateByUrl("/matches");
				},
				error: (error: { error?: { error?: string; detail?: string } }) => {
					this.isDevLoginLoading = false;
					this.errorMessage =
						error.error?.error ??
						error.error?.detail ??
						"Development login failed.";
				},
			});
	}

	async ngAfterViewInit(): Promise<void> {
		const clientId = this.getGoogleClientId();
		if (!clientId) {
			this.isLoading = false;
			if (!this.isDevLoginEnabled) {
				this.errorMessage = "Google Client ID is not configured for the frontend.";
			}
			return;
		}

		try {
			const container = this.googleButtonContainer?.nativeElement;
			if (!container) {
				throw new Error("Google Identity Services failed to initialize.");
			}

			await this.googleIdentityService.renderSignInButton(
				clientId,
				container,
				({ credential }) => {
					if (!credential) {
						this.errorMessage = "Google did not return an ID token.";
						return;
					}

					void this.router.navigateByUrl("/auth/callback", {
						state: { idToken: credential },
					});
				},
			);

			this.isLoading = false;
		} catch (error) {
			this.isLoading = false;
			this.errorMessage =
				error instanceof Error
					? error.message
					: "Unable to load Google sign-in.";
		}
	}

	private getGoogleClientId(): string {
		return window.__env?.googleClientId?.trim() ?? "";
	}
}
