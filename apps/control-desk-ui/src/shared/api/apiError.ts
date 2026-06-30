export type ApiErrorItem = {
  code: string;
  message: string;
  target?: string | null;
};

export class ApiError extends Error {
  readonly statusCode: number;
  readonly errors: ApiErrorItem[];

  constructor(statusCode: number, message: string, errors: ApiErrorItem[] = []) {
    super(message);
    this.name = "ApiError";
    this.statusCode = statusCode;
    this.errors = errors;
  }
}
