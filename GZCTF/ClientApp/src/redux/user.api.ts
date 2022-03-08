import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

export interface User {
  name: string;
  descr: string;
  studentId: string;
  realName: string;
  isSYSU: boolean;
  phone: string;
  email: string;
}

export interface UserRegisterDto {
  userName: string;
  password: string;
  email: string;
}

export interface UserLoginDto {
  userName: string;
  password: string;
}

export interface UserRecoveryDto {
  email: string;
}

export interface UserResetPasswordDto {
  password: string;
  email: string;
  rToken: string;
}

export interface UserVerifyEmailDto {
  token: string;
  email: string;
}

export interface UserUpdateInfoDto {
  userName?: string;
  descr?: string;
  studentId?: string;
  phoneNumber?: string;
  realName?: string;
}

export interface UserChangePasswordDto {
  old: string;
  new: string;
}

export interface UserChangeEmailDto {
  newMail: string;
}

export interface UserConfirmChangingEmailDto {
  token: string;
  email: string;
}

export const USER_API = createApi({
  reducerPath: 'userApi',
  tagTypes: ['User'],
  baseQuery: fetchBaseQuery({ baseUrl: 'api/account' }),
  endpoints: (builder) => ({
    status: builder.query<User, void>({
      query: () => ({
        url: 'me',
        method: 'POST'
      }),
      providesTags: ['User'],
    }),
    register: builder.mutation<void, UserRegisterDto>({
      query: (dto) => ({
        url: 'register',
        method: 'POST',
        body: dto
      })
    }),
    login: builder.mutation<void, UserLoginDto>({
      query: (dto) => ({
        url: 'login',
        method: 'POST',
        body: dto
      })
    }),
    recovery: builder.mutation<void, UserRecoveryDto>({
      query: (dto) => ({
        url: 'recovery',
        method: 'POST',
        body: dto
      })
    }),
    resetPassword: builder.mutation<void, UserResetPasswordDto>({
      query: (dto) => ({
        url: 'passwordreset',
        method: 'POST',
        body: dto
      })
    }),
    verifyEmail: builder.mutation<void, UserVerifyEmailDto>({
      query: (dto) => ({
        url: 'verify',
        method: 'POST',
        body: dto
      })
    }),
    logout: builder.mutation<void, void>({
      query: () => ({
        url: 'logout',
        method: 'POST'
      }),
      invalidatesTags: ['User']
    }),
    updateInfo: builder.mutation<void, UserUpdateInfoDto>({
      query: (dto) => ({
        url: 'update',
        method: 'PUT',
        body: dto
      }),
      invalidatesTags: ['User']
    }),
    changePassword: builder.mutation<void, UserChangePasswordDto>({
      query: (dto) => ({
        url: 'changepassword',
        method: 'PUT',
        body: dto
      })
    }),
    changeEmail: builder.mutation<void, UserChangeEmailDto>({
      query: (dto) => ({
        url: 'changeemail',
        method: 'PUT',
        body: dto
      })
    }),
    confirmChangingEmail: builder.mutation<void, UserConfirmChangingEmailDto>({
      query: (dto) => ({
        url: 'mailchangeconfirm',
        method: 'POST',
        body: dto
      })
    })
  })
});
