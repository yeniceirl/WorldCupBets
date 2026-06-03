import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import type { Observable } from "rxjs";
import type { CreateUserInvitationRequest, CreateUserInvitationResult } from "./admin.models";

@Injectable({ providedIn: "root" })
export class AdminService {
	private readonly httpClient = inject(HttpClient);

	createInvitation(request: CreateUserInvitationRequest): Observable<CreateUserInvitationResult> {
		return this.httpClient.post<CreateUserInvitationResult>("/api/admin/invitations", request);
	}
}
