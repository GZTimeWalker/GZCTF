import React, { FC } from 'react'
import { ScrollArea, Tabs } from '@mantine/core'
import {
  mdiAccountCogOutline,
  mdiBullhornOutline,
  mdiFlagOutline,
  mdiAccountGroupOutline,
  mdiFileDocumentOutline,
} from '@mdi/js'
import Icon from '@mdi/react'
import { Role } from '../Api'
import WithNavBar from '../components/WithNavbar'
import WithRole from '../components/WithRole'
import GameManager from '../components/admin/GameManager'
import LogViewer from '../components/admin/LogViewer'
import NoticeManager from '../components/admin/NoticeManager'
import TeamManager from '../components/admin/TeamManager'
import UserManager from '../components/admin/UserManager'

const Admin: FC = () => {
  return (
    <WithNavBar>
      <WithRole requiredRole={Role.Admin}>
        <Tabs grow tabPadding="md" position="apart">
          <Tabs.Tab label="通知管理" icon={<Icon path={mdiBullhornOutline} size={1} />}>
            <NoticeManager />
          </Tabs.Tab>

          <Tabs.Tab label="比赛管理" icon={<Icon path={mdiFlagOutline} size={1} />}>
            <GameManager />
          </Tabs.Tab>

          <Tabs.Tab label="队伍管理" icon={<Icon path={mdiAccountGroupOutline} size={1} />}>
            <TeamManager />
          </Tabs.Tab>

          <Tabs.Tab label="用户管理" icon={<Icon path={mdiAccountCogOutline} size={1} />}>
            <UserManager />
          </Tabs.Tab>

          <Tabs.Tab label="系统日志" icon={<Icon path={mdiFileDocumentOutline} size={1} />}>
            <LogViewer />
          </Tabs.Tab>
        </Tabs>
      </WithRole>
    </WithNavBar>
  )
}

export default Admin
