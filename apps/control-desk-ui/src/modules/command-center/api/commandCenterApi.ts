import { apiRequest } from "../../../shared/api/httpClient";
import type {
  ClientQueueFilter,
  ClientQueueSort,
  ClientWorkQueuePage
} from "../utils/commandCenterQueueModel";

export async function listClientWorkQueuePage(
  input: {
    lane?: ClientQueueFilter;
    search?: string;
    sort?: ClientQueueSort;
    take?: number;
    cursor?: string;
  } = {}
): Promise<ClientWorkQueuePage> {
  const search = new URLSearchParams();

  if (input.lane !== undefined) {
    search.set("lane", input.lane);
  }

  if (input.search?.trim()) {
    search.set("search", input.search.trim());
  }

  if (input.sort !== undefined) {
    search.set("sort", input.sort);
  }

  if (input.take !== undefined) {
    search.set("take", String(input.take));
  }

  if (input.cursor?.trim()) {
    search.set("cursor", input.cursor.trim());
  }

  const query = search.toString();
  const page = await apiRequest<ClientWorkQueuePage>(
    `/api/v1/command-center/client-work${query === "" ? "" : `?${query}`}`
  );

  if (page.summary === undefined || page.pageSize === undefined || page.hasMore === undefined) {
    throw new Error("Office Control API must be upgraded before client work pages can be read.");
  }

  return page;
}
