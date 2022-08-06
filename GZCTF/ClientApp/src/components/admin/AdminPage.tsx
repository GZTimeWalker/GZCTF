import { Role } from '@Api/Api'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import React, { FC } from 'react'
import WithAdminTab from './WithAdminTab'
import { AdminTabProps } from './WithAdminTab'

const AdminPage: FC<AdminTabProps> = (props) => {
  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.Admin}>
        <WithAdminTab {...props} />
      </WithRole>
    </WithNavBar>
  )
}

export default AdminPage
