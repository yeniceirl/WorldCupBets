import { provideHttpClient, withInterceptors } from "@angular/common/http";
import {
	type ApplicationConfig,
	ENVIRONMENT_INITIALIZER,
	inject,
	provideBrowserGlobalErrorListeners,
} from "@angular/core";
import { provideAnimations } from "@angular/platform-browser/animations";
import { provideRouter } from "@angular/router";
import { appRoutes } from "./app.routes";
import { AuthStateService } from "./core/auth/auth-state.service";
import { authTokenInterceptor } from "./core/auth/auth-token.interceptor";

export const appConfig: ApplicationConfig = {
	providers: [
		provideBrowserGlobalErrorListeners(),
		provideAnimations(),
		provideHttpClient(withInterceptors([authTokenInterceptor])),
		provideRouter(appRoutes),
		{
			provide: ENVIRONMENT_INITIALIZER,
			multi: true,
			useValue: () => inject(AuthStateService).hydrateFromStorage(),
		},
	],
};
