import { Injectable, computed, signal } from "@angular/core";

type Theme = "light" | "dark";

const THEME_KEY = "worldcupbets.theme";

@Injectable({ providedIn: "root" })
export class ThemeService {
	readonly theme = signal<Theme>(this.loadInitialTheme());
	readonly isDark = computed(() => this.theme() === "dark");

	constructor() {
		this.applyTheme(this.theme());
	}

	toggle(): void {
		const nextTheme = this.isDark() ? "light" : "dark";
		this.theme.set(nextTheme);
		localStorage.setItem(THEME_KEY, nextTheme);
		this.applyTheme(nextTheme);
	}

	private loadInitialTheme(): Theme {
		const storedTheme = localStorage.getItem(THEME_KEY);
		if (storedTheme === "light" || storedTheme === "dark") {
			return storedTheme;
		}

		return window.matchMedia?.("(prefers-color-scheme: dark)").matches ? "dark" : "light";
	}

	private applyTheme(theme: Theme): void {
		document.documentElement.classList.toggle("dark", theme === "dark");
	}
}
