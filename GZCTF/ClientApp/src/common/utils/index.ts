import { SerializedError } from "@reduxjs/toolkit";
import { FetchBaseQueryError } from "@reduxjs/toolkit/dist/query";

export function or(condition: boolean, classNames: string) {
  return condition ? classNames : '';
}

export function resolveMessage(error: FetchBaseQueryError | SerializedError) {
  return (error as any).data?.title || (error as any).message || '未知错误';
}

export function resolveMessageForRateLimit(error: FetchBaseQueryError | SerializedError) {
  return (error as any).data?.title || (error as any).message || '过于频繁';
}
