/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { LocalFile } from '../models/LocalFile';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class AssetsService {

    /**
     * 获取文件接口
     * 根据哈希获取文件，不匹配文件名
     * @param hash 文件哈希
     * @param filename 下载文件名
     * @returns any 成功获取文件
     * @throws ApiError
     */
    public static assetsGetFile(
        hash: string,
        filename: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/Assets/{hash}/{filename}',
            path: {
                'hash': hash,
                'filename': filename,
            },
            errors: {
                404: `文件未找到`,
            },
        });
    }

    /**
     * 上传文件接口
     * 上传一个或多个文件
     * @param formData
     * @returns LocalFile 成功上传文件
     * @throws ApiError
     */
    public static assetsUpload(
        formData?: {
            files?: Array<Blob>;
        },
    ): CancelablePromise<Array<LocalFile>> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Assets',
            formData: formData,
            mediaType: 'multipart/form-data',
            errors: {
                400: `上传文件失败`,
                401: `未授权用户`,
                403: `无权访问`,
            },
        });
    }

    /**
     * 删除文件接口
     * 按照文件哈希删除文件
     * @param hash
     * @returns any 成功删除文件
     * @throws ApiError
     */
    public static assetsDelete(
        hash: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/Assets/{hash}',
            path: {
                'hash': hash,
            },
            errors: {
                400: `上传文件失败`,
                401: `未授权用户`,
                403: `无权访问`,
            },
        });
    }

}