import type { NextPage } from 'next';
import { useState } from 'react';
import { Text, Stack, Group, Title, Center } from '@mantine/core';
import LogoHeader from '../components/LogoHeader';
import RichTextEditor from '../components/RichText';
import WithNavBar from '../components/WithNavbar';
import MainIcon from '../components/icon/MainIcon';

const initialValue =
  '<p>Your initial <b>html value</b> or an empty string to init editor without value</p>';

const About: NextPage = () => {
  const [value, onChange] = useState(initialValue);

  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
        <Text>
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Minima nostrum soluta vitae ea
          nulla cum autem possimus dicta provident aliquam. Quibusdam, consequatur eligendi dolorum
          illo impedit quos dolores aut fuga.
        </Text>
        <Text>
          励轮抛胖村镀杰盟贼杂锐矛听邦廊烫溅钒逸啥植匀辽烙详躯插炊唯哗，乡棒茧钨扔滨叛斥唉糠教虽逼！炮犬尽吹搏脸纺谓斑镰节塘觉恶功；顾昌预酚劲沙丽画苗肖土灯噪烤凯总伙痰泼匹紧？雾沫政叫颁岭灶度兰抖熄写并匝拣着优兆兴耘碧逆蛴辩焕势害偶走池祛田宜摇搞古陡均奇临搂凑士泄奸伟吵嚼依矮盒疽上豆循。
        </Text>
        <Text>
          昼孢途敞弓萎每肿甩功亿犁锡厉诗悦犯葱屁求，仆探斋慨庸力样绕查柏滔塑相厚蠕教铜挪闷存挝腕烂柜脖湿课睦洁彼贯都碧算窝继佣旱螬疤沙甸嗽茁酱庭顿茅策疮砚总，盼俗近、剖可比掩宋锈凋惑罩废乖黎班诺钠秸槽？穆感渴浑址涛赚迷寺脑眶盘笔愤淮参轴炸腔筹盈凸蚴电职递？疗够熊冻葱笛协剑硅脱吞，嗓之匆翼场配汽怒胡惊叭稠？梯，伟拦悔搬拥掷纱黔疾搅术窃桥入乏结殿脸虾道溢匆蹈盈汁瞎温套衡俗妙慧已稿？役泥扯此小诞仔琼贸滑碱辆医雇矛芽迅礁算推七饵客卓终陋释岭财源傍沼扒皂罗零戏径牲怎央初差丁都茧坑实杠圈筑萝白十垃功纽啉奋条、长牌扩辟禽亿塌王，鳞恩顺戒坟错扯党病它泪高父探射淤先岭营衰均，辈论享益铜炭板总廊鼓及功杰马送纠譬措人定蜘谣揭魂拟睛呀导踏后厩痢悉饮提慈糟洼哩丁说、蕾国秦越期坠吨离绥滋，凭玛拖妥不烦稗穴通韦担陨兆屯纺屯润就立复刷颂耳桃惨裁昨焚聪吸。
        </Text>
        <Text size="xs">
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Aspernatur soluta fuga explicabo.
          Perferendis culpa atque quisquam soluta hic molestias similique quas id. Quae incidunt
          possimus sit minus veniam, repellat officia.
        </Text>
        <Text size="sm">
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Aspernatur magni illum assumenda
          perspiciatis, dicta quam tempore sit dolorem, repellendus necessitatibus distinctio
          consequuntur ad ipsa, excepturi eligendi aut vel voluptas quidem.
        </Text>
        <Text size="md">
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Neque, assumenda consequuntur?
          Tempora doloremque provident sit praesentium quo, repellat eligendi expedita saepe minus
          aspernatur nobis vero sint error perferendis iure nulla.
        </Text>
        <Text size="lg">
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Aperiam aspernatur molestias,
          blanditiis nobis sed eum. Culpa aspernatur earum blanditiis deleniti, dolor repellat vero
          facere recusandae deserunt quod quidem, eos magni!
        </Text>
        <Text size="xl">
          Lorem ipsum dolor sit amet consectetur adipisicing elit. Quae dolorem sunt soluta
          necessitatibus. Consequatur nulla minus, perspiciatis sint, ab aliquam ipsam magni illum
          ipsum, voluptatem exercitationem expedita doloribus. Error, ad?
        </Text>
        <Center>
          <RichTextEditor style={{ width: '95%', height: 500 }} value={value} onChange={onChange} />
        </Center>
      </Stack>
    </WithNavBar>
  );
};

export default About;
