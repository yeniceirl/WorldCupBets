import { inject } from "@angular/core";
import { type CanActivateFn, Router } from "@angular/router";
import { AuthStateService } from "./auth-state.service";

export const authGuard: CanActivateFn = () => {
	const authState = inject(AuthStateService);
	if (authState.isAuthenticated()) {
		return true;
	}

	return inject(Router).createUrlTree(["/auth/login"]);
};
