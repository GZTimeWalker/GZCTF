import { FC } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, Tabs } from '@mantine/core'
import {
  mdiAccountMultipleCheckOutline,
  mdiPencilOutline,
  mdiBullhornOutline,
  mdiFlagOutline,
  mdiBackburger,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import AdminPage from '../../../../components/admin/AdminPage'
import GameInfo from '../../../../components/admin/games/GameInfo'

const GameEdit: FC = () => {
  const navigate = useNavigate()
  return (
    <AdminPage
      headProps={{ position: 'left' }}
      head={
        <Button
          leftIcon={<Icon path={mdiBackburger} size={1} />}
          onClick={() => navigate('/admin/games')}
        >
          返回上级
        </Button>
      }
    >
      <Tabs
        orientation="vertical"
        styles={{
          root: {
            display: 'flex',
          },
          tabsListWrapper: {
            width: '8rem',
          },
          body: {
            width: 'calc(100% - 10rem)',
          },
        }}
      >
        <Tabs.Tab label="信息编辑" icon={<Icon path={mdiPencilOutline} size={1} />}>
          <GameInfo />
        </Tabs.Tab>
        <Tabs.Tab label="比赛通知" icon={<Icon path={mdiBullhornOutline} size={1} />}>
          Messages tab content
        </Tabs.Tab>
        <Tabs.Tab label="题目编辑" icon={<Icon path={mdiFlagOutline} size={1} />}>
          Settings tab content
        </Tabs.Tab>
        <Tabs.Tab label="队伍审核" icon={<Icon path={mdiAccountMultipleCheckOutline} size={1} />}>
          Settings tab content
        </Tabs.Tab>
      </Tabs>
    </AdminPage>
  )
}

export default GameEdit
