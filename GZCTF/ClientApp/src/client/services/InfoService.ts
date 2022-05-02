/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { Notice } from '../models/Notice';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class InfoService {

    /**
     * 获取最新公告
     * 获取最新公告
     * @returns Notice 成功获取公告
     * @throws ApiError
     */
    public static infoGetNotices(): CancelablePromise<Array<Notice>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/notices',
        });
    }

}