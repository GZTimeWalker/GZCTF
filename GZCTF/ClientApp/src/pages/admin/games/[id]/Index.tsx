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
import ChallengeEdit from '../../../../components/admin/games/ChallengeEdit'
import GameInfo from '../../../../components/admin/games/GameInfo'
import GameNotice from '../../../../components/admin/games/GameNotice'
import TeamReview from '../../../../components/admin/games/TeamReview'

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
          <GameNotice />
        </Tabs.Tab>
        <Tabs.Tab label="题目编辑" icon={<Icon path={mdiFlagOutline} size={1} />}>
          <ChallengeEdit />
        </Tabs.Tab>
        <Tabs.Tab label="队伍审核" icon={<Icon path={mdiAccountMultipleCheckOutline} size={1} />}>
          <TeamReview />
        </Tabs.Tab>
      </Tabs>
    </AdminPage>
  )
}

export default GameEdit
